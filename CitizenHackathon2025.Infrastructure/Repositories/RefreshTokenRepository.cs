using System.Data;
using Dapper;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
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

        // ========== CREATE ==========
        public Task AddAsync(RefreshToken refreshToken)
        {
                const string sql = @"
                    INSERT INTO [RefreshTokens] (Token, Email, ExpiryDate, Status, IsRevoked)
                    VALUES (@Token, @Email, @ExpiryDate, @Status, CASE WHEN @Status = @Revoked THEN 1 ELSE 0 END);";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Token", refreshToken.Token, DbType.String);
            parameters.Add("Email", refreshToken.Email, DbType.String);
            parameters.Add("ExpiryDate", refreshToken.ExpiryDate, DbType.DateTime2);
            parameters.Add("Status", (int)refreshToken.Status, DbType.Int32);
            parameters.Add("Revoked", (int)RefreshTokenStatus.Revoked, DbType.Int32);

            return _connection.ExecuteAsync(sql, parameters);
        }

        // ========== UPDATE ==========
        public Task UpdateStatusAsync(int id, RefreshTokenStatus status)
        {
            const string sql = @"
                    UPDATE [RefreshTokens]
                    SET Status = @Status,
                        IsRevoked = CASE WHEN @Status = @Revoked THEN 1
                                         WHEN @Status = @Active  THEN 0
                                         ELSE IsRevoked END
                    +WHERE Id = @Id;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", id);
            parameters.Add("status", (int)status);
            parameters.Add("revoked", (int)RefreshTokenStatus.Revoked);
            parameters.Add("active", (int)RefreshTokenStatus.Active);

            return _connection.ExecuteAsync(sql, parameters);
        }


        public Task RevokeAsync(string token)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status = @Revoked, IsRevoked = 1
                WHERE Token = @Token;",
                new { Token = token, Revoked = (int)RefreshTokenStatus.Revoked });

        public Task ExpireAsync(string token)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status = @Expired
                WHERE Token = @Token;",
                new { Token = token, Expired = (int)RefreshTokenStatus.Expired });

        public Task DeactivateTokenAsync(int id)
            => _connection.ExecuteAsync(@"
                UPDATE [RefreshTokens]
                SET Status = @Revoked, IsRevoked = 1
                WHERE Id = @Id;",
                new { Id = id, Revoked = (int)RefreshTokenStatus.Revoked });
    }
}