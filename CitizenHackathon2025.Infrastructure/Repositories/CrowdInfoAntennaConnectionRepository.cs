using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CrowdInfoAntennaConnectionRepository : ICrowdInfoAntennaConnectionRepository
    {
        private readonly IDbConnection _db;
        public CrowdInfoAntennaConnectionRepository(IDbConnection db) => _db = db;

        public async Task UpsertPingAsync(
            int antennaId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            string? band,
            string? additionalJson,
            CancellationToken ct)
        {
            // MERGE on (AntennaId, DeviceHash) thanks to your unique constraint UQ
            const string sql = @"
                            DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

                            MERGE dbo.CrowdInfoAntennaConnection AS T
                            USING (SELECT @AntennaId AS AntennaId, @DeviceHash AS DeviceHash) AS S
                            ON (T.AntennaId = S.AntennaId AND T.DeviceHash = S.DeviceHash)
                            WHEN MATCHED THEN
                                UPDATE SET
                                    T.LastSeenUtc = @NowUtc,
                                    T.Active = 1,
                                    T.IpHash = COALESCE(@IpHash, T.IpHash),
                                    T.MacHash = COALESCE(@MacHash, T.MacHash),
                                    T.Source = @Source,
                                    T.SignalStrength = @SignalStrength,
                                    T.Band = @Band,
                                    T.AdditionalJson = @AdditionalJson
                            WHEN NOT MATCHED THEN
                                INSERT (AntennaId, DeviceHash, IpHash, MacHash, Source, SignalStrength, Band, FirstSeenUtc, LastSeenUtc, Active, AdditionalJson)
                                VALUES (@AntennaId, @DeviceHash, @IpHash, @MacHash, @Source, @SignalStrength, @Band, @NowUtc, @NowUtc, 1, @AdditionalJson);";
            
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@AntennaId", antennaId, DbType.Int32);
            parameters.Add("@DeviceHash", deviceHash, DbType.Binary);
            parameters.Add("@IpHash", ipHash, DbType.Binary);
            parameters.Add("@MacHash", macHash, DbType.Binary);
            parameters.Add("@Source", source, DbType.Byte);
            parameters.Add("@SignalStrength", signalStrength, DbType.Int16);
            parameters.Add("@Band", band, DbType.String);
            parameters.Add("@AdditionalJson", additionalJson, DbType.String);

            await _db.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
        }

        public async Task<(int activeConnections, int uniqueDevices)> GetCountsAsync(
            int antennaId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken ct)
        {
            const string sql = @"
                            SELECT
                                COUNT_BIG(*) AS ActiveConnections,
                                COUNT_BIG(DISTINCT DeviceHash) AS UniqueDevices
                            FROM dbo.CrowdInfoAntennaConnection
                            WHERE Active = 1
                              AND AntennaId = @AntennaId
                              AND LastSeenUtc >= @WindowStartUtc
                              AND LastSeenUtc <  @WindowEndUtc;";

            var row = await _db.QuerySingleAsync<dynamic>(
                new CommandDefinition(sql, new { AntennaId = antennaId, WindowStartUtc = windowStartUtc, WindowEndUtc = windowEndUtc }, cancellationToken: ct));

            return ((int)row.ActiveConnections, (int)row.UniqueDevices);
        }

        public async Task<long> CreateAsync(
            int antennaId,
            int? eventId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            short? rssi,
            string? band,
            string? additionalJson,
            CancellationToken ct)
                {
                    const string sql = @"
                                    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

                                    INSERT INTO dbo.CrowdInfoAntennaConnection
                                    (
                                        AntennaId, EventId, DeviceHash, IpHash, MacHash,
                                        Source, SignalStrength, Band, FirstSeenUtc, LastSeenUtc, Rssi, Active, AdditionalJson
                                    )
                                    VALUES
                                    (
                                        @AntennaId, @EventId, @DeviceHash, @IpHash, @MacHash,
                                        @Source, @SignalStrength, @Band, @NowUtc, @NowUtc, @Rssi, 1, @AdditionalJson
                                    );

                                    SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@AntennaId", antennaId, DbType.Int32);
            parameters.Add("@EventId", eventId, DbType.Int32);
            parameters.Add("@DeviceHash", deviceHash, DbType.Binary);
            parameters.Add("@IpHash", ipHash, DbType.Binary);
            parameters.Add("@MacHash", macHash, DbType.Binary);
            parameters.Add("@Source", source, DbType.Byte);
            parameters.Add("@SignalStrength", signalStrength, DbType.Int16);
            parameters.Add("@Band", band, DbType.String);
            parameters.Add("@Rssi", rssi, DbType.Int16);
            parameters.Add("@AdditionalJson", additionalJson, DbType.String);

            var id = await _db.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, parameters));

            return id;
        }

        public async Task<int> ArchiveAndDeleteExpiredAsync(int timeoutSeconds, int batchSize, CancellationToken ct)
        {
            const string sql = "EXEC dbo.ArchiveAndDeleteExpiredAntennaConnections @TimeoutSeconds, @BatchSize;";
            return await _db.ExecuteAsync(new CommandDefinition(sql, new { TimeoutSeconds = timeoutSeconds, BatchSize = batchSize }, cancellationToken: ct));
        }

        //public async Task<IReadOnlyList<DeletedAntennaConnectionDTO>> GetDeletedAsync(
        //    int antennaId,
        //    DateTime sinceUtc,
        //    int take,
        //    long? cursorDeletedId,
        //    CancellationToken ct)
        //{
        //    take = Math.Clamp(take, 1, 500);

        //    // Simple “cursor” pagination via DeletedId (stable, fast)
        //    const string sql = @"
        //                    SELECT TOP (@Take)
        //                        DeletedId, OriginalId, AntennaId, EventId,
        //                        DeviceHash, IpHash, MacHash,
        //                        Source, SignalStrength, Band,
        //                        FirstSeenUtc, LastSeenUtc, Rssi,
        //                        AdditionalJson,
        //                        DeletedUtc, DeletedReason
        //                    FROM dbo.CrowdInfoAntennaConnection_Deleted
        //                    WHERE AntennaId = @AntennaId
        //                      AND DeletedUtc >= @SinceUtc
        //                      AND (@CursorDeletedId IS NULL OR DeletedId < @CursorDeletedId)
        //                    ORDER BY DeletedId DESC;";

        //    var rows = await _db.QueryAsync<DeletedAntennaConnectionDTO>(
        //        new CommandDefinition(sql, new
        //        {
        //            AntennaId = antennaId,
        //            SinceUtc = sinceUtc,
        //            Take = take,
        //            CursorDeletedId = cursorDeletedId
        //        }, cancellationToken: ct));

        //    return rows.AsList();
        //}

        public async Task<int> PurgeDeletedArchiveAsync(int retentionDays, int batchSize, CancellationToken ct)
        {
            retentionDays = Math.Clamp(retentionDays, 1, 3650);
            batchSize = Math.Clamp(batchSize, 100, 50_000);

            // Batch purging (avoids large dreadlocks)
            const string sql = @"
                            DECLARE @Cutoff DATETIME2(3) = DATEADD(DAY, -ABS(@RetentionDays), SYSUTCDATETIME());

                            ;WITH cte AS
                            (
                                SELECT TOP (@BatchSize) DeletedId
                                FROM dbo.CrowdInfoAntennaConnection_Deleted WITH (READPAST, ROWLOCK)
                                WHERE DeletedUtc < @Cutoff
                                ORDER BY DeletedUtc ASC
                            )
                            DELETE d
                            FROM dbo.CrowdInfoAntennaConnection_Deleted d
                            JOIN cte ON cte.DeletedId = d.DeletedId;

                            SELECT @@ROWCOUNT;";

            var deleted = await _db.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { RetentionDays = retentionDays, BatchSize = batchSize }, cancellationToken: ct));

            return deleted;
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.