using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CrowdAlertVoteRepository : ICrowdAlertVoteRepository
    {
        private readonly IDbConnection _connection;

        public CrowdAlertVoteRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task InsertAsync(CrowdAlertVote vote, CancellationToken ct = default)
        {
            const string sql = @"
                            INSERT INTO dbo.CrowdAlertVote
                            (
                                PlaceId,
                                ZoneKey,
                                UserId,
                                DeviceHash,
                                IpHash,
                                Latitude,
                                Longitude,
                                Reason,
                                CreatedAtUtc,
                                Active
                            )
                            VALUES
                            (
                                @PlaceId,
                                @ZoneKey,
                                @UserId,
                                @DeviceHash,
                                @IpHash,
                                @Latitude,
                                @Longitude,
                                @Reason,
                                SYSUTCDATETIME(),
                                1
                            );";

            await _connection.ExecuteAsync(
                new CommandDefinition(sql, vote, cancellationToken: ct));
        }

        public async Task<int> CountDistinctReportersAsync(
            string zoneKey,
            int windowMinutes,
            CancellationToken ct = default)
        {
            const string sql = @"
                            DECLARE @SinceUtc DATETIME2(3) =
                                DATEADD(MINUTE, -@WindowMinutes, SYSUTCDATETIME());

                            SELECT
                                COUNT(DISTINCT COALESCE(
                                    CAST(UserId AS NVARCHAR(64)),
                                    DeviceHash,
                                    IpHash
                                )) AS DistinctReporterCount
                            FROM dbo.CrowdAlertVote
                            WHERE ZoneKey = @ZoneKey
                              AND CreatedAtUtc >= @SinceUtc
                              AND Active = 1;";

            return await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        ZoneKey = zoneKey,
                        WindowMinutes = windowMinutes
                    },
                    cancellationToken: ct));
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.