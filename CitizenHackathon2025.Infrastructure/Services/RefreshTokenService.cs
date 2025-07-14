using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Citizenhackathon2025.Infrastructure.Services
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

        public async Task<string> GenerateAsync(string email)
        {
            var token = Guid.NewGuid().ToString("N"); // random secure token
            var refreshToken = new RefreshToken
            {
                Token = token,
                Email = email,
                ExpiryDate = DateTime.UtcNow.Add(_tokenLifetime),
                IsRevoked = false
            };

            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO RefreshTokens (Id, Token, Email, ExpiryDate, IsRevoked, CreatedAt)
                        VALUES (@Id, @Token, @Email, @ExpiryDate, @IsRevoked, @CreatedAt)";
            await connection.ExecuteAsync(sql, refreshToken);

            _cache.Set(token, refreshToken, _tokenLifetime);

            return token;
        }

        public async Task<bool> ValidateAsync(string token)
        {
            if (_cache.TryGetValue<RefreshToken>(token, out var cachedToken))
            {
                return !cachedToken.IsRevoked && cachedToken.ExpiryDate > DateTime.UtcNow;
            }

            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM RefreshTokens WHERE Token = @Token";
            var dbToken = await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { Token = token });

            if (dbToken == null || dbToken.IsRevoked || dbToken.ExpiryDate <= DateTime.UtcNow)
                return false;

            _cache.Set(token, dbToken, dbToken.ExpiryDate - DateTime.UtcNow);
            return true;
        }

        public async Task InvalidateAsync(string token)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token";
            await connection.ExecuteAsync(sql, new { Token = token });

            _cache.Remove(token);
        }

        public async Task<IEnumerable<RefreshToken>> GetTokensForUserAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM RefreshTokens WHERE Email = @Email ORDER BY CreatedAt DESC";
            return await connection.QueryAsync<RefreshToken>(sql, new { Email = email });
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.