using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Enums.CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Models;
using CitizenHackathon2025.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Data;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class EmergencyAlertRepository : IEmergencyAlertRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public EmergencyAlertRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        // =====================================================
        // APPLY / UPSERT
        // =====================================================

        public async Task<EmergencyAlertApplyResult> ApplyAsync(EmergencyAlert alert, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alert);

            cancellationToken.ThrowIfCancellationRequested();

            using var connection = await OpenConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                var existing = await FindForUpdateAsync(connection, transaction, alert.SourceCode, alert.ExternalId, cancellationToken);
                /*
                 * Exact same CAP payload:
                 * nothing to update and nothing to broadcast.
                 */
                if (existing is not null && string.Equals(existing.PayloadHash, alert.PayloadHash, StringComparison.OrdinalIgnoreCase) && existing.Status.Equals(alert.Status))
                {
                    transaction.Commit();
                    return new EmergencyAlertApplyResult(StoredAlert: existing, Changed: false, IsActive: existing.IsActive, RemovedAlerts: Array.Empty<EmergencyAlertRemoval>());
                }
                /*
                 * Same SourceCode + ExternalId:
                 * preserve the OutZen Id.
                 */
                if (existing is not null)
                {
                    alert.Id = existing.Id;
                    alert.CreatedAtUtc = existing.CreatedAtUtc;
                }
                else if (alert.Id == Guid.Empty)
                {
                    alert.Id = Guid.NewGuid();
                }

                var now = DateTimeOffset.UtcNow;

                if (alert.CreatedAtUtc == default)
                {
                    alert.CreatedAtUtc = now;
                }

                alert.UpdatedAtUtc = now;

                var removed = new List<EmergencyAlertRemoval>();

                // =================================================
                // CAP UPDATE / SUPERSESSION
                // =================================================

                if (IsActiveStatus(alert) && alert.ReferencedExternalIds.Count > 0)
                {
                    foreach (var referenceId in alert.ReferencedExternalIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var previous = await FindForUpdateAsync(connection, transaction, alert.SourceCode, referenceId, cancellationToken);

                        if (previous is null || !previous.IsActive)
                        {
                            continue;
                        }
                        await DeactivateAsync(connection, transaction, previous.Id, supersededById: alert.Id, newStatus: null, cancellationToken);

                        previous.IsActive = false;
                        previous.UpdatedAtUtc = now;

                        removed.Add(new EmergencyAlertRemoval(previous, EmergencyAlertRemovalReason.Superseded));
                    }
                }

                // =================================================
                // CAP CANCEL
                // =================================================

                if (IsCancelledStatus(alert))
                {
                    foreach (var referenceId in alert.ReferencedExternalIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var previous = await FindForUpdateAsync(connection, transaction, alert.SourceCode, referenceId, cancellationToken);

                        if (previous is null || !previous.IsActive)
                        {
                            continue;
                        }

                        await DeactivateAsync(connection, transaction, previous.Id, supersededById: alert.Id, newStatus: alert.Status, cancellationToken);

                        previous.IsActive = false;
                        previous.Status = alert.Status;
                        previous.UpdatedAtUtc = now;

                        removed.Add(new EmergencyAlertRemoval(previous, EmergencyAlertRemovalReason.Cancelled));
                    }
                }
                var isActive = IsActiveStatus(alert) && (!alert.ExpiresAtUtc.HasValue || alert.ExpiresAtUtc.Value > now);
                alert.IsActive = isActive;
                await UpsertRowAsync(connection, transaction, alert, cancellationToken);
                transaction.Commit();

                return new EmergencyAlertApplyResult(StoredAlert: alert, Changed: true, IsActive: isActive, RemovedAlerts: removed);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =====================================================
        // ACTIVE ALERTS
        // =====================================================

        public async Task<IReadOnlyList<EmergencyAlert>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            const string sql = """
                            SELECT
                                Id,
                                SourceCode,
                                ExternalId,
                                ExternalReferenceId,
                                ReferencedExternalIdsJson,
                                CorrelationKey,

                                HazardType,
                                Severity,
                                Urgency,
                                Certainty,
                                Status,
                                InformationKind,

                                Headline,
                                Description,
                                Instructions,
                                Language,

                                SentAtUtc,
                                EffectiveFromUtc,
                                ExpiresAtUtc,
                                LastUpdatedAtUtc,

                                AreaWkt,
                                AreaSrid,
                                RadiusMeters,

                                ProvinceCode,
                                MunicipalityCode,
                                OfficialInformationUri,

                                IsOfficial,
                                IsMachineVerified,

                                PayloadHash,
                                RawPayloadStorageKey,

                                IsActive,
                                SupersededById,

                                CreatedAtUtc,
                                UpdatedAtUtc

                            FROM dbo.EmergencyAlert

                            WHERE IsActive = 1
                              AND
                              (
                                  ExpiresAtUtc IS NULL
                                  OR ExpiresAtUtc > SYSUTCDATETIME()
                              )

                            ORDER BY
                                Severity DESC,
                                EffectiveFromUtc DESC;
                            """;

            using var connection = await OpenConnectionAsync(cancellationToken);
            var rows = await connection.QueryAsync<EmergencyAlertRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));

            return rows.Select(ToEntity).ToList();
        }

        // =====================================================
        // EXPIRATION
        // =====================================================

        public async Task<IReadOnlyList<EmergencyAlert>> ExpireDueAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await OpenConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                const string selectSql = """
                                    SELECT
                                        Id,
                                        SourceCode,
                                        ExternalId,
                                        ExternalReferenceId,
                                        ReferencedExternalIdsJson,
                                        CorrelationKey,

                                        HazardType,
                                        Severity,
                                        Urgency,
                                        Certainty,
                                        Status,
                                        InformationKind,

                                        Headline,
                                        Description,
                                        Instructions,
                                        Language,

                                        SentAtUtc,
                                        EffectiveFromUtc,
                                        ExpiresAtUtc,
                                        LastUpdatedAtUtc,

                                        AreaWkt,
                                        AreaSrid,
                                        RadiusMeters,

                                        ProvinceCode,
                                        MunicipalityCode,
                                        OfficialInformationUri,

                                        IsOfficial,
                                        IsMachineVerified,

                                        PayloadHash,
                                        RawPayloadStorageKey,

                                        IsActive,
                                        SupersededById,

                                        CreatedAtUtc,
                                        UpdatedAtUtc

                                    FROM dbo.EmergencyAlert
                                        WITH (UPDLOCK, HOLDLOCK)

                                    WHERE IsActive = 1
                                        AND ExpiresAtUtc IS NOT NULL
                                        AND ExpiresAtUtc <= @NowUtc;
                                    """;

                var rows =
                    (
                        await connection.QueryAsync<EmergencyAlertRow>(
                            new CommandDefinition(
                                selectSql,
                                new
                                {
                                    NowUtc = nowUtc
                                },
                                transaction,
                                cancellationToken:cancellationToken)
                        )
                    )
                    .ToList();

                if (rows.Count == 0)
                {
                    transaction.Commit();
                    return Array.Empty<EmergencyAlert>();
                }

                var expiredStatus = RequiredEnumValue< EmergencyAlertStatus>("Expired");

                const string updateSql = """
                                    UPDATE dbo.EmergencyAlert

                                    SET
                                        IsActive = 0,
                                        Status = @Status,
                                        UpdatedAtUtc = @UpdatedAtUtc

                                    WHERE Id = @Id
                                        AND IsActive = 1;
                                    """;

                var expired = new List<EmergencyAlert>(rows.Count);

                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            updateSql,
                            new
                            {
                                row.Id,
                                Status = Convert.ToInt32(expiredStatus),
                                UpdatedAtUtc = nowUtc
                            },
                            transaction,
                            cancellationToken: cancellationToken
                        ));

                    var entity = ToEntity(row);

                    entity.IsActive = false;
                    entity.Status = expiredStatus;
                    entity.UpdatedAtUtc = nowUtc;

                    expired.Add(entity);
                }

                transaction.Commit();

                return expired;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =====================================================
        // FIND WITH LOCK
        // =====================================================

        private static async Task<EmergencyAlert?> FindForUpdateAsync(IDbConnection connection, IDbTransaction transaction, string sourceCode, string externalId, CancellationToken cancellationToken)
        {
            const string sql = """
                            SELECT
                                Id,
                                SourceCode,
                                ExternalId,
                                ExternalReferenceId,
                                ReferencedExternalIdsJson,
                                CorrelationKey,

                                HazardType,
                                Severity,
                                Urgency,
                                Certainty,
                                Status,
                                InformationKind,

                                Headline,
                                Description,
                                Instructions,
                                Language,

                                SentAtUtc,
                                EffectiveFromUtc,
                                ExpiresAtUtc,
                                LastUpdatedAtUtc,

                                AreaWkt,
                                AreaSrid,
                                RadiusMeters,

                                ProvinceCode,
                                MunicipalityCode,
                                OfficialInformationUri,

                                IsOfficial,
                                IsMachineVerified,

                                PayloadHash,
                                RawPayloadStorageKey,

                                IsActive,
                                SupersededById,

                                CreatedAtUtc,
                                UpdatedAtUtc

                            FROM dbo.EmergencyAlert
                                WITH (UPDLOCK, HOLDLOCK)

                            WHERE SourceCode = @SourceCode
                              AND ExternalId = @ExternalId;
                            """;


            var row = await connection.QuerySingleOrDefaultAsync<EmergencyAlertRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        SourceCode = sourceCode,

                        ExternalId = externalId
                    },
                    transaction,
                    cancellationToken: cancellationToken));


            return row is null ? null : ToEntity(row);
        }


        // =====================================================
        // DEACTIVATE
        // =====================================================

        private static Task DeactivateAsync(IDbConnection connection, IDbTransaction transaction, Guid id, Guid? supersededById, EmergencyAlertStatus? newStatus, CancellationToken cancellationToken)
        {
            const string sql = """
                            UPDATE dbo.EmergencyAlert

                            SET
                                IsActive = 0,

                                SupersededById =
                                    @SupersededById,

                                Status =
                                    COALESCE(
                                        @NewStatus,
                                        Status),

                                UpdatedAtUtc =
                                    SYSUTCDATETIME()

                            WHERE Id = @Id
                                AND IsActive = 1;
                            """;


            return connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id,
                        SupersededById = supersededById,
                        NewStatus = newStatus.HasValue ? Convert.ToInt32(newStatus.Value) : (int?)null
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }


        // =====================================================
        // UPSERT ROW
        // =====================================================

        private static Task UpsertRowAsync(IDbConnection connection, IDbTransaction transaction, EmergencyAlert alert, CancellationToken cancellationToken)
        {
            var areaWkt = alert.Area?.AsText();
            var referencedExternalIdsJson = JsonSerializer.Serialize(alert.ReferencedExternalIds);

            const string sql = """
                            UPDATE dbo.EmergencyAlert

                            SET
                                ExternalReferenceId = @ExternalReferenceId,
                                ReferencedExternalIdsJson = @ReferencedExternalIdsJson,
                                CorrelationKey = @CorrelationKey,
                                HazardType = @HazardType,
                                Severity = @Severity,
                                Urgency = @Urgency,
                                Certainty = @Certainty,
                                Status = @Status,
                                InformationKind = @InformationKind,
                                Headline = @Headline,
                                Description = @Description,
                                Instructions = @Instructions,
                                Language = @Language,
                                SentAtUtc = @SentAtUtc,
                                EffectiveFromUtc = @EffectiveFromUtc,
                                ExpiresAtUtc = @ExpiresAtUtc,
                                LastUpdatedAtUtc = @LastUpdatedAtUtc,
                                AreaWkt = @AreaWkt,
                                AreaSrid = @AreaSrid,
                                RadiusMeters = @RadiusMeters,
                                ProvinceCode = @ProvinceCode,
                                MunicipalityCode = @MunicipalityCode,
                                OfficialInformationUri = @OfficialInformationUri,
                                IsOfficial = @IsOfficial,
                                IsMachineVerified = @IsMachineVerified,
                                PayloadHash = @PayloadHash,
                                RawPayloadStorageKey = @RawPayloadStorageKey,
                                UpdatedAtUtc = @UpdatedAtUtc,
                                IsActive = @IsActive

                            WHERE SourceCode = @SourceCode
                              AND ExternalId = @ExternalId;

                            IF @@ROWCOUNT = 0
                            BEGIN
                                INSERT INTO dbo.EmergencyAlert
                                (
                                    Id,

                                    SourceCode,
                                    ExternalId,

                                    ExternalReferenceId,
                                    ReferencedExternalIdsJson,
                                    CorrelationKey,

                                    HazardType,
                                    Severity,
                                    Urgency,
                                    Certainty,
                                    Status,
                                    InformationKind,

                                    Headline,
                                    Description,
                                    Instructions,
                                    Language,

                                    SentAtUtc,
                                    EffectiveFromUtc,
                                    ExpiresAtUtc,
                                    LastUpdatedAtUtc,

                                    AreaWkt,
                                    AreaSrid,
                                    RadiusMeters,

                                    ProvinceCode,
                                    MunicipalityCode,

                                    OfficialInformationUri,

                                    IsOfficial,
                                    IsMachineVerified,

                                    PayloadHash,
                                    RawPayloadStorageKey,

                                    IsActive,

                                    SupersededById,

                                    CreatedAtUtc,
                                    UpdatedAtUtc
                                )
                                VALUES
                                (
                                    @Id,

                                    @SourceCode,
                                    @ExternalId,

                                    @ExternalReferenceId,
                                    @ReferencedExternalIdsJson,
                                    @CorrelationKey,

                                    @HazardType,
                                    @Severity,
                                    @Urgency,
                                    @Certainty,
                                    @Status,
                                    @InformationKind,

                                    @Headline,
                                    @Description,
                                    @Instructions,
                                    @Language,

                                    @SentAtUtc,
                                    @EffectiveFromUtc,
                                    @ExpiresAtUtc,
                                    @LastUpdatedAtUtc,

                                    @AreaWkt,
                                    @AreaSrid,
                                    @RadiusMeters,

                                    @ProvinceCode,
                                    @MunicipalityCode,

                                    @OfficialInformationUri,

                                    @IsOfficial,
                                    @IsMachineVerified,

                                    @PayloadHash,
                                    @RawPayloadStorageKey,

                                    @IsActive,

                                    NULL,

                                    @CreatedAtUtc,
                                    @UpdatedAtUtc
                                );

                            END;
                            """;


            var parameters =
                new
                {
                    alert.Id,

                    alert.SourceCode,
                    alert.ExternalId,

                    alert.ExternalReferenceId,

                    ReferencedExternalIdsJson = referencedExternalIdsJson,

                    alert.CorrelationKey,
                    HazardType = Convert.ToInt32(alert.HazardType),
                    Severity = Convert.ToInt32(alert.Severity),
                    Urgency = Convert.ToInt32(alert.Urgency),
                    Certainty = Convert.ToInt32(alert.Certainty),
                    Status = Convert.ToInt32(alert.Status),
                    InformationKind = Convert.ToInt32(alert.InformationKind),

                    alert.Headline,
                    alert.Description,
                    alert.Instructions,
                    alert.Language,

                    alert.SentAtUtc,
                    alert.EffectiveFromUtc,
                    alert.ExpiresAtUtc,
                    alert.LastUpdatedAtUtc,

                    AreaWkt = areaWkt,

                    AreaSrid = alert.Area?.SRID > 0 ? alert.Area.SRID : 4326,

                    alert.RadiusMeters,

                    alert.ProvinceCode,
                    alert.MunicipalityCode,

                    OfficialInformationUri = alert.OfficialInformationUri?.ToString(),

                    alert.IsOfficial,
                    alert.IsMachineVerified,

                    alert.PayloadHash,
                    alert.RawPayloadStorageKey,

                    alert.IsActive,

                    alert.CreatedAtUtc,
                    alert.UpdatedAtUtc
                };

            return connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
        }


        // =====================================================
        // CONNECTION
        // =====================================================

        private async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = _connectionFactory.CreateConnection();

            if (connection is SqlConnection sqlConnection)
            {
                await sqlConnection.OpenAsync(cancellationToken);

                return sqlConnection;
            }

            connection.Open();

            return connection;
        }

        // =====================================================
        // ROW -> DOMAIN MODEL
        // =====================================================
        private static EmergencyAlert ToEntity(EmergencyAlertRow row)
        {
            Geometry? area = null;

            if (!string.IsNullOrWhiteSpace(row.AreaWkt))
            {
                try
                {
                    var reader = new WKTReader();

                    area = (Geometry)reader.Read(row.AreaWkt);

                    area.SRID = row.AreaSrid > 0 ? row.AreaSrid : 4326;
                }
                catch
                {
                    area = null;
                }
            }

            IReadOnlyList<string> referencedExternalIds = Array.Empty<string>();


            if (!string.IsNullOrWhiteSpace(row.ReferencedExternalIdsJson))
            {
                try
                {
                    referencedExternalIds = JsonSerializer.Deserialize<string[]>(row.ReferencedExternalIdsJson) ?? Array.Empty<string>();
                }
                catch
                {
                    referencedExternalIds = Array.Empty<string>();
                }
            }

            Uri? officialUri = null;

            if (!string.IsNullOrWhiteSpace(row.OfficialInformationUri))
            {
                Uri.TryCreate(row.OfficialInformationUri, UriKind.Absolute, out officialUri);
            }


            return new EmergencyAlert
            {
                Id = row.Id,
                SourceCode = row.SourceCode,
                ExternalId = row.ExternalId,
                ExternalReferenceId = row.ExternalReferenceId,
                ReferencedExternalIds = referencedExternalIds,
                CorrelationKey = row.CorrelationKey,
                HazardType = (EmergencyHazardType)row.HazardType,
                Severity = (EmergencySeverity)row.Severity,
                Urgency = (EmergencyUrgency)row.Urgency,
                Certainty = (EmergencyCertainty)row.Certainty,
                Status = (EmergencyAlertStatus)row.Status,
                InformationKind = (SafetyInformationKind)row.InformationKind,
                Headline = row.Headline,
                Description = row.Description,
                Instructions = row.Instructions,
                Language = row.Language,
                SentAtUtc = row.SentAtUtc,
                EffectiveFromUtc = row.EffectiveFromUtc,
                ExpiresAtUtc = row.ExpiresAtUtc,
                LastUpdatedAtUtc = row.LastUpdatedAtUtc,
                Area = area,
                RadiusMeters = row.RadiusMeters,
                ProvinceCode = row.ProvinceCode,
                MunicipalityCode = row.MunicipalityCode,
                OfficialInformationUri = officialUri,
                IsOfficial = row.IsOfficial,
                IsMachineVerified = row.IsMachineVerified,
                IsActive = row.IsActive,
                PayloadHash = row.PayloadHash,
                RawPayloadStorageKey = row.RawPayloadStorageKey,
                CreatedAtUtc = row.CreatedAtUtc,
                UpdatedAtUtc = row.UpdatedAtUtc
            };
        }
        private static TEnum RequiredEnumValue<TEnum>(params string[] candidates) where TEnum : struct, Enum
        {
            foreach (var candidate in candidates)
            {
                if (Enum.TryParse<TEnum>(candidate, ignoreCase: true, out var result))
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"None of the expected values " + $"[{string.Join(", ", candidates)}] " + $"exists in enum " + $"{typeof(TEnum).Name}.");
        }
        private static bool IsActiveStatus(EmergencyAlert alert)
        {
            return string.Equals(alert.Status.ToString(), "Active", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsCancelledStatus(EmergencyAlert alert)
        {
            return string.Equals(alert.Status.ToString(), "Cancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(alert.Status.ToString(), "Canceled", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class EmergencyAlertRow
        {
            public Guid Id { get; set; }
            public string SourceCode { get; set; } = string.Empty;
            public string ExternalId { get; set; } = string.Empty;
            public string? ExternalReferenceId { get; set; }
            public string? ReferencedExternalIdsJson { get; set; }
            public string? CorrelationKey { get; set; }
            public int HazardType { get; set; }
            public int Severity { get; set; }
            public int Urgency { get; set; }
            public int Certainty { get; set; }
            public int Status { get; set; }
            public int InformationKind { get; set; }
            public string Headline { get; set; } = string.Empty;
            public string Description { get; set; }  = string.Empty;
            public string? Instructions { get; set; }
            public string Language { get; set; } = "fr-BE";
            public DateTimeOffset SentAtUtc { get; set; }
            public DateTimeOffset EffectiveFromUtc { get; set; }
            public DateTimeOffset? ExpiresAtUtc { get; set; }
            public DateTimeOffset LastUpdatedAtUtc { get; set; }
            public string? AreaWkt { get; set; }
            public int AreaSrid { get; set; }
            public double? RadiusMeters { get; set; }
            public string? ProvinceCode { get; set; }
            public string? MunicipalityCode { get; set; }
            public string? OfficialInformationUri { get; set; }
            public bool IsOfficial { get; set; }
            public bool IsMachineVerified { get; set; }
            public string PayloadHash { get; set; } = string.Empty;
            public string? RawPayloadStorageKey { get; set; }
            public bool IsActive { get; set; }
            public Guid? SupersededById { get; set; }
            public DateTimeOffset CreatedAtUtc { get; set; }
            public DateTimeOffset UpdatedAtUtc { get; set; }
        }
    }
}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.