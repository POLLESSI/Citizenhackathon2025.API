using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
#nullable disable
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly TimeSpan _tokenLifetime = TimeSpan.FromDays(1);

        public RefreshTokenService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Generates a new refresh token and persists it.
        /// </summary>
        public async Task<RefreshToken> GenerateAsync(string email)
        {
            var token = Guid.NewGuid().ToString("N");

            var refreshToken = new RefreshToken
            {
                Token = token,
                Email = email,
                ExpiryDate = DateTime.UtcNow.Add(_tokenLifetime),
                CreatedAt = DateTime.UtcNow,
                Status = RefreshTokenStatus.Active
            };

            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO RefreshTokens (Token, Email, ExpiryDate, CreatedAt, Status)
                        VALUES (@Token, @Email, @ExpiryDate, @CreatedAt, @Status)";
            await connection.ExecuteAsync(sql, refreshToken);

            _cache.Set(token, refreshToken, _tokenLifetime);

            return refreshToken;
        }

        /// <summary>
        /// Checks if a token is valid (status + expiration).
        /// </summary>
        public async Task<bool> ValidateAsync(string token)
        {
            if (_cache.TryGetValue<RefreshToken>(token, out var cachedToken))
            {
                return cachedToken.IsActive();
            }

            using var connection = new SqlConnection(_connectionString);
            var sql = @"SELECT * 
                        FROM RefreshTokens 
                        WHERE Token = @Token 
                          AND Status = @Status";

            var dbToken = await connection.QuerySingleOrDefaultAsync<RefreshToken>(
                sql, new { Token = token, Status = RefreshTokenStatus.Active });

            if (dbToken == null || !dbToken.IsActive())
                return false;

            _cache.Set(token, dbToken, dbToken.ExpiryDate - DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Revokes a token (Revoked status).
        /// </summary>
        public async Task InvalidateAsync(string token)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE RefreshTokens 
                        SET Status = @Status 
                        WHERE Token = @Token";

            await connection.ExecuteAsync(sql, new { Token = token, Status = RefreshTokenStatus.Revoked });

            _cache.Remove(token);
        }

        /// <summary>
        /// Expires a token explicitly (Expired status).
        /// </summary>
        public async Task ExpireAsync(string token)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE RefreshTokens 
                        SET Status = @Status 
                        WHERE Token = @Token";

            await connection.ExecuteAsync(sql, new { Token = token, Status = RefreshTokenStatus.Expired });

            _cache.Remove(token);
        }

        /// <summary>
        /// Deactivates a token via its ID (typically on the Admin side).
        /// </summary>
        public async Task DeactivateTokenAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE RefreshTokens 
                        SET Status = @Status 
                        WHERE Id = @Id";

            await connection.ExecuteAsync(sql, new { Id = id, Status = RefreshTokenStatus.Revoked });
        }

        /// <summary>
        /// Returns the status of a given token.
        /// </summary>
        public async Task<RefreshTokenStatus> GetStatusAsync(string token)
        {
            if (_cache.TryGetValue<RefreshToken>(token, out var cachedToken))
            {
                return cachedToken.Status;
            }

            using var connection = new SqlConnection(_connectionString);
            var sql = @"SELECT Status 
                        FROM RefreshTokens 
                        WHERE Token = @Token";

            var status = await connection.ExecuteScalarAsync<int?>(sql, new { Token = token });
            return status.HasValue
                ? (RefreshTokenStatus)status.Value
                : RefreshTokenStatus.Revoked; // fallback if token does not exist
        }

        /// <summary>
        /// Retrieves all tokens of a user.
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetTokensForUserAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"SELECT * 
                        FROM RefreshTokens 
                        WHERE Email = @Email 
                        ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<RefreshToken>(sql, new { Email = email });
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.