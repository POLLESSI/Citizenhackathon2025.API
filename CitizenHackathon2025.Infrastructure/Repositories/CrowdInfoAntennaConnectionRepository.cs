using System.Data;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;

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
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.