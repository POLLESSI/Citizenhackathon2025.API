using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CrowdSafetyAlertRepository : ICrowdSafetyAlertRepository
    {
        private readonly IDbConnection _db;

        public CrowdSafetyAlertRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<CrowdSafetyAlert> InsertAsync(CrowdSafetyAlert alert, CancellationToken ct = default)
        {
            const string sql = """
                            INSERT INTO dbo.CrowdSafetyAlert
                            (
                                AntennaId,
                                EventId,
                                Severity,
                                Status,
                                ActiveConnections,
                                UniqueDevices,
                                BaselineConnections,
                                IsRural,
                                IsNight,
                                IsKnownEvent,
                                IsSensitiveZone,
                                Latitude,
                                Longitude,
                                Title,
                                Message,
                                DetectedAtUtc,
                                Active
                            )
                            OUTPUT
                                inserted.Id,
                                inserted.AntennaId,
                                inserted.EventId,
                                inserted.Severity,
                                inserted.Status,
                                inserted.ActiveConnections,
                                inserted.UniqueDevices,
                                inserted.BaselineConnections,
                                inserted.IsRural,
                                inserted.IsNight,
                                inserted.IsKnownEvent,
                                inserted.IsSensitiveZone,
                                inserted.Latitude,
                                inserted.Longitude,
                                inserted.Title,
                                inserted.Message,
                                inserted.DetectedAtUtc,
                                inserted.ValidatedAtUtc,
                                inserted.ValidatedByUserId,
                                inserted.Active
                            VALUES
                            (
                                @AntennaId,
                                @EventId,
                                @Severity,
                                @Status,
                                @ActiveConnections,
                                @UniqueDevices,
                                @BaselineConnections,
                                @IsRural,
                                @IsNight,
                                @IsKnownEvent,
                                @IsSensitiveZone,
                                @Latitude,
                                @Longitude,
                                @Title,
                                @Message,
                                @DetectedAtUtc,
                                @Active
                            );
                        """;

            return await _db.QuerySingleAsync<CrowdSafetyAlert>(
                new CommandDefinition(sql, alert, cancellationToken: ct));
        }

        public async Task<IReadOnlyList<CrowdSafetyAlert>> GetLatestAsync(int limit = 50, CancellationToken ct = default)
        {
            const string sql = """
                            SELECT TOP(@Limit)
                                Id,
                                AntennaId,
                                EventId,
                                Severity,
                                Status,
                                ActiveConnections,
                                UniqueDevices,
                                BaselineConnections,
                                IsRural,
                                IsNight,
                                IsKnownEvent,
                                IsSensitiveZone,
                                Latitude,
                                Longitude,
                                Title,
                                Message,
                                DetectedAtUtc,
                                ValidatedAtUtc,
                                ValidatedByUserId,
                                Active
                            FROM dbo.CrowdSafetyAlert
                            WHERE Active = 1
                            ORDER BY DetectedAtUtc DESC;
                        """;

            var rows = await _db.QueryAsync<CrowdSafetyAlert>(
                new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));

            return rows.AsList();
        }

        public async Task<int?> GetBaselineConnectionsAsync(int antennaId, DateTime nowUtc, CancellationToken ct = default)
        {
            const string sql = """
                            DECLARE @CurrentHour INT = DATEPART(HOUR, @NowUtc);

                            SELECT CAST(AVG(CAST(ActiveConnections AS FLOAT)) AS INT) AS BaselineConnections
                            FROM dbo.CrowdInfoAntennaSnapshot
                            WHERE AntennaId = @AntennaId
                              AND WindowStartUtc >= DATEADD(HOUR, -24, @NowUtc)
                              AND WindowStartUtc < @NowUtc
                              AND DATEPART(HOUR, WindowStartUtc) = @CurrentHour;
                            """;

            return await _db.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        AntennaId = antennaId,
                        NowUtc = nowUtc
                    },
                    cancellationToken: ct));
        }

        public async Task<IReadOnlyList<CrowdSafetyAlert>> GetPendingRemindersAsync(int limit, CancellationToken ct)
        {
            limit = Math.Clamp(limit, 1, 500);

            const string sql = "dbo.CrowdSafetyAlert_GetPendingReminders";

            var rows = await _db.QueryAsync<CrowdSafetyAlert>(
                new CommandDefinition(
                    sql,
                    new { Limit = limit },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

            return rows.AsList();
        }

        public async Task<bool> HasRecentSimilarAlertAsync(int antennaId, byte minSeverity, TimeSpan cooldown, CancellationToken ct = default)
        {
            const string sql = """
                            SELECT COUNT(1)
                            FROM dbo.CrowdSafetyAlert
                            WHERE Active = 1
                              AND AntennaId = @AntennaId
                              AND Severity >= @MinSeverity
                              AND DetectedAtUtc >= DATEADD(SECOND, -@CooldownSeconds, SYSUTCDATETIME());
                            """;

            var count = await _db.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        AntennaId = antennaId,
                        MinSeverity = minSeverity,
                        CooldownSeconds = (int)cooldown.TotalSeconds
                    },
                    cancellationToken: ct));

            return count > 0;
        }

        public Task<int> ValidateAsync(long alertId, int validatedByUserId, CancellationToken ct = default)
        {
            const string sql = """
                            UPDATE dbo.CrowdSafetyAlert
                            SET Status = 'Validated',
                                ValidatedAtUtc = SYSUTCDATETIME(),
                                ValidatedByUserId = @ValidatedByUserId
                            WHERE Id = @AlertId
                              AND Active = 1;
                            """;

            return _db.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        AlertId = alertId,
                        ValidatedByUserId = validatedByUserId
                    },
                    cancellationToken: ct));
        }

        public async Task MarkReminderSentAsync(long alertId, CancellationToken ct)
        {
            const string sql = @"
                            UPDATE dbo.CrowdSafetyAlert
                            SET
                                ReminderCount = ISNULL(ReminderCount, 0) + 1,
                                LastReminderAtUtc = SYSUTCDATETIME()
                            WHERE Id = @AlertId
                              AND Active = 1;";

            await _db.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { AlertId = alertId },
                    cancellationToken: ct));
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.