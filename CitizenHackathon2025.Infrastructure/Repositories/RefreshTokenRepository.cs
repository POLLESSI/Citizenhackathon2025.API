using System.Data;
using Dapper;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    /// <summary>
    /// Dapper implementation for the [RefreshTokens] table.
    /// Compatible with your schema (Id, Token, Email, ExpiryDate, IsRevoked, CreatedAt, Status).
    /// </summary>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnection _connection;
        public RefreshTokenRepository(IDbConnection connection) => _connection = connection;

        // ========== READ ==========
        public Task<RefreshToken?> GetByIdAsync(int id)
            => _connection.QueryFirstOrDefaultAsync<RefreshToken?>(
                "SELECT TOP(1) * FROM [RefreshTokens] WHERE Id = @Id",
                new { Id = id
            });

        public Task<RefreshToken?> GetByTokenAsync(string token)
            => _connection.QueryFirstOrDefaultAsync<RefreshToken?>(
                "SELECT TOP(1) * FROM [RefreshTokens] WHERE Token = @Token",
                new { Token = token
            });

        public Task<IEnumerable<RefreshToken>> GetByEmailAsync(string email)
            => _connection.QueryAsync<RefreshToken>(
                "SELECT * FROM [RefreshTokens] WHERE Email = @Email ORDER BY CreatedAt DESC",
                new { Email = email 
            });

        public Task<IEnumerable<RefreshToken>> GetActiveByEmailAsync(string email)
    => _connection.QueryAsync<RefreshToken>(@"
        SELECT *
        FROM [RefreshTokens]
        WHERE Email = @Email
          AND Status = @Active
          AND ExpiryDate > SYSUTCDATETIME()
        ORDER BY CreatedAt DESC",
        new { Email = email, Active = (int)RefreshTokenStatus.Active });

        // hash/salt writing (without clear token)
        public Task AddHashedAsync(string email, DateTime expiryDate, byte[] tokenHash, byte[] tokenSalt)
            => _connection.ExecuteAsync(@"
                INSERT INTO [RefreshTokens](Email, ExpiryDate, Status, IsRevoked, CreatedAt, TokenHash, TokenSalt)
                VALUES (@Email, @ExpiryDate, @Status, 0, SYSUTCDATETIME(), @TokenHash, @TokenSalt)",
                new
                {
                    Email = email,
                    ExpiryDate = expiryDate,
                    Status = (int)RefreshTokenStatus.Active,
                    TokenHash = tokenHash,
                    TokenSalt = tokenSalt
                });

        // ========== CREATE ==========
        public Task AddAsync(RefreshToken refreshToken)
            => _connection.ExecuteAsync(@"
                INSERT INTO [RefreshTokens]
                    (Token, Email, ExpiryDate, Status, IsRevoked, CreatedAt, TokenHash, TokenSalt)
                VALUES
                    (@Token, @Email, @ExpiryDate, @Status, CASE WHEN @Status=@Revoked THEN 1 ELSE 0 END, SYSUTCDATETIME(), @TokenHash, @TokenSalt)",
                new
                {
                    refreshToken.Token,           // if column kept during migration
                    refreshToken.Email,
                    refreshToken.ExpiryDate,
                    Status = (int)refreshToken.Status,
                    Revoked = (int)RefreshTokenStatus.Revoked,
                    refreshToken.TokenHash,
                    refreshToken.TokenSalt
                });

        // ========== UPDATE ==========
        public Task UpdateStatusAsync(int id, RefreshTokenStatus status)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status   = @status,
                    IsRevoked = CASE WHEN @status = @revoked THEN 1
                                     WHEN @status = @active  THEN 0
                                     ELSE IsRevoked END
                WHERE Id = @id;",
                new
                {
                    id,
                    status = (int)status,
                    revoked = (int)RefreshTokenStatus.Revoked,
                    active = (int)RefreshTokenStatus.Active
                });


        public Task RevokeAsync(string token)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status=@Revoked, IsRevoked=1
                WHERE Token=@Token;",
                new { Token = token, Revoked = (int)RefreshTokenStatus.Revoked });

        public Task ExpireAsync(string token)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status=@Expired
                WHERE Token=@Token;",
                new { Token = token, Expired = (int)RefreshTokenStatus.Expired });

        public Task DeactivateTokenAsync(int id)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status=@Revoked, IsRevoked=1
                WHERE Id=@Id;",
                new { Id = id, Revoked = (int)RefreshTokenStatus.Revoked });
    }
}



















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.