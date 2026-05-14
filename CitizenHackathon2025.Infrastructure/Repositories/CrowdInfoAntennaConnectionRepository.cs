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
            int? eventId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            string? band,
            string? additionalJson,
            CancellationToken ct)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@AntennaId", antennaId, DbType.Int32);
            parameters.Add("@EventId", eventId, DbType.Int32);
            parameters.Add("@DeviceHash", deviceHash, DbType.Binary, size: 32);
            parameters.Add("@IpHash", ipHash, DbType.Binary, size: 32);
            parameters.Add("@MacHash", macHash, DbType.Binary, size: 32);
            parameters.Add("@Source", source, DbType.Byte);
            parameters.Add("@SignalStrength", signalStrength, DbType.Int16);
            parameters.Add("@Band", band, DbType.String, size: 16);
            parameters.Add("@AdditionalJson", additionalJson, DbType.String);

            await _db.ExecuteAsync(new CommandDefinition(
                commandText: "dbo.sp_AntennaConnection_Ping",
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        }

        public async Task<(int activeConnections, int uniqueDevices)> GetCountsAsync(int antennaId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken ct)
        {
            const string sql = @"
                            SELECT
                                CAST(COUNT_BIG(*) AS int) AS ActiveConnections,
                                CAST(COUNT_BIG(DISTINCT DeviceHash) AS int) AS UniqueDevices
                            FROM dbo.CrowdInfoAntennaConnection
                            WHERE Active = 1
                              AND AntennaId = @AntennaId
                              AND LastSeenUtc >= @WindowStartUtc
                              AND LastSeenUtc <  @WindowEndUtc;";

            var row = await _db.QuerySingleAsync<AntennaCountRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        AntennaId = antennaId,
                        WindowStartUtc = windowStartUtc,
                        WindowEndUtc = windowEndUtc
                    },
                    cancellationToken: ct));

            return (row.ActiveConnections, row.UniqueDevices);
        }

        private sealed class AntennaCountRow
        {
            public int ActiveConnections { get; set; }
            public int UniqueDevices { get; set; }
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

            var rows = await _db.QueryAsync<dynamic>(
                new CommandDefinition(sql, new { TimeoutSeconds = timeoutSeconds, BatchSize = batchSize }, cancellationToken: ct));

            return rows.Count();
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

        public async Task<object> DebugDbAsync()
        {
            const string sql = """
        SELECT 
            @@SERVERNAME AS ServerName,
            DB_NAME() AS CurrentDatabase,
            SUSER_SNAME() AS LoginName;
        """;

            return await _db.QuerySingleAsync(sql);
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.