using System.Data;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Queries;
using Dapper;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class UserSessionRepository : IUserSessionRepository
    {
        private readonly IDbConnection _cn;
        public UserSessionRepository(IDbConnection cn) => _cn = cn;

        public Task UpsertAsync(UserSession s) => _cn.ExecuteAsync(@"
                                                            MERGE dbo.UserSessions WITH (HOLDLOCK) AS t
                                                            USING (SELECT @Jti AS Jti) AS src
                                                            ON (t.Jti = src.Jti)
                                                            WHEN MATCHED THEN UPDATE SET
                                                                LastSeenUtc=@LastSeenUtc, ExpiresAtUtc=@ExpiresAtUtc, Source=@Source, Ip=@Ip, UserAgent=@UserAgent
                                                            WHEN NOT MATCHED THEN INSERT
                                                                (UserEmail,Jti,RefreshFamilyId,IssuedAtUtc,ExpiresAtUtc,LastSeenUtc,Source,Ip,UserAgent,IsRevoked)
                                                            VALUES
                                                                (@UserEmail,@Jti,@RefreshFamilyId,@IssuedAtUtc,@ExpiresAtUtc,@LastSeenUtc,@Source,@Ip,@UserAgent,0);",s);

        public Task TouchAsync(string jti, DateTime nowUtc) =>
            _cn.ExecuteAsync(@"UPDATE dbo.UserSessions SET LastSeenUtc=@nowUtc WHERE Jti=@jti AND IsRevoked=0;",
                new { jti, nowUtc });

        public Task<bool> IsRevokedAsync(string jti) =>
            _cn.ExecuteScalarAsync<bool>(
                                "SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.UserSessions WHERE Jti=@jti AND IsRevoked=1) THEN 1 ELSE 0 END;",
                                new { jti });

        public Task<int> RevokeAsync(string jti, string reason) =>
            _cn.ExecuteAsync("UPDATE dbo.UserSessions SET IsRevoked=1 WHERE Jti=@jti;", new { jti });

        public async Task<IEnumerable<UserSession>> QueryAsync(SessionQuery q)
        {
            var sql = @"
                    SELECT *
                    FROM dbo.UserSessions
                    WHERE (@Email IS NULL OR UserEmail = @Email)
                      AND (@Jti   IS NULL OR Jti = @Jti)
                      AND (@From IS NULL OR LastSeenUtc >= @From)
                      AND (@To   IS NULL OR LastSeenUtc <  @To)
                      AND (@OnlyActive = 0 OR (IsRevoked = 0 AND ExpiresAtUtc > SYSUTCDATETIME()))
                    ORDER BY LastSeenUtc DESC
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;";
            return await _cn.QueryAsync<UserSession>(sql, new
            {
                Email = q.Email,
                Jti = q.Jti,
                From = q.FromUtc,
                To = q.ToUtc,
                OnlyActive = q.OnlyActive ? 1 : 0,
                q.Skip,
                q.Take
            });
        }

        public Task<int> PurgeExpiredAsync() =>
            _cn.ExecuteAsync("DELETE FROM dbo.UserSessions WHERE ExpiresAtUtc < DATEADD(day,-1,SYSUTCDATETIME());");
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.