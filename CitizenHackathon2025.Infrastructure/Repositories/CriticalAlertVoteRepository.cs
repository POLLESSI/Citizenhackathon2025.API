using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CriticalAlertVoteRepository : ICriticalAlertVoteRepository
    {
        private readonly IDbConnection _db;

        public CriticalAlertVoteRepository(IDbConnection db)
        {
            _db = db;
        }

        public Task InsertAsync(CriticalAlertVote vote, CancellationToken ct = default)
        {
            const string sql = """
                            INSERT INTO dbo.CriticalAlertVote
                            (
                                AlertKind,
                                ZoneKey,
                                PlaceId,
                                UserId,
                                DeviceHash,
                                IpHash,
                                Latitude,
                                Longitude,
                                Reason
                            )
                            VALUES
                            (
                                @AlertKind,
                                @ZoneKey,
                                @PlaceId,
                                @UserId,
                                @DeviceHash,
                                @IpHash,
                                @Latitude,
                                @Longitude,
                                @Reason
                            );
                        """;

            return _db.ExecuteAsync(new CommandDefinition(sql, vote, cancellationToken: ct));
        }

        public Task<int> CountDistinctReportersAsync(CriticalAlertKind alertKind, string zoneKey, int windowMinutes, CancellationToken ct = default)
        {
            const string sql = """
                            SELECT COUNT(DISTINCT ReporterKey)
                            FROM
                            (
                                SELECT
                                    CASE
                                        WHEN NULLIF(
                                            LTRIM(RTRIM(DeviceHash)),
                                            N''
                                        ) IS NOT NULL
                                        THEN
                                            N'D:' + DeviceHash

                                        WHEN UserId IS NOT NULL
                                        THEN
                                            N'U:' +
                                            CONVERT(
                                                NVARCHAR(64),
                                                UserId
                                            )

                                        WHEN NULLIF(
                                            LTRIM(RTRIM(IpHash)),
                                            N''
                                        ) IS NOT NULL
                                        THEN
                                            N'I:' + IpHash

                                        ELSE NULL
                                    END AS ReporterKey
                                FROM dbo.CriticalAlertVote
                                WHERE AlertKind = @AlertKind
                                  AND ZoneKey = @ZoneKey
                                  AND CreatedAtUtc >= DATEADD(
                                        MINUTE,
                                        -@WindowMinutes,
                                        SYSUTCDATETIME()
                                      )
                                  AND Active = 1
                            ) AS Reporters
                            WHERE ReporterKey IS NOT NULL;
                            """;

            return _db.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        AlertKind =
                            (byte)alertKind,

                        ZoneKey =
                            zoneKey,

                        WindowMinutes =
                            windowMinutes
                    },
                    cancellationToken:
                        ct));
        }
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.