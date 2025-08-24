using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(IDbConnection connection, ILogger<RefreshTokenRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<RefreshToken?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM RefreshTokens WHERE Id = @Id";
            return await _connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { Id = id });
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            var sql = "SELECT * FROM RefreshTokens WHERE Token = @Token";
            return await _connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { Token = token });
        }

        public async Task<IEnumerable<RefreshToken>> GetByEmailAsync(string email)
        {
            var sql = "SELECT * FROM RefreshTokens WHERE Email = @Email ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<RefreshToken>(sql, new { Email = email });
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            var sql = @"INSERT INTO RefreshTokens (Token, Email, ExpiryDate, CreatedAt, Status)
                        VALUES (@Token, @Email, @ExpiryDate, @CreatedAt, @Status)";
            await _connection.ExecuteAsync(sql, refreshToken);

            _logger.LogInformation("Refresh token created for {Email}", refreshToken.Email);
        }

        public async Task UpdateStatusAsync(int id, RefreshTokenStatus status)
        {
            var sql = "UPDATE RefreshTokens SET Status = @Status WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, new { Id = id, Status = status });

            _logger.LogInformation("Refresh token {Id} status updated to {Status}", id, status);
        }

        public async Task RevokeAsync(string token)
        {
            var sql = "UPDATE RefreshTokens SET Status = @Status WHERE Token = @Token";
            await _connection.ExecuteAsync(sql, new { Token = token, Status = RefreshTokenStatus.Revoked });

            _logger.LogInformation("Refresh token {Token} has been revoked.", token);
        }

        public async Task ExpireAsync(string token)
        {
            var sql = "UPDATE RefreshTokens SET Status = @Status WHERE Token = @Token";
            await _connection.ExecuteAsync(sql, new { Token = token, Status = RefreshTokenStatus.Expired });

            _logger.LogInformation("Refresh token {Token} has been marked as expired.", token);
        }

        public async Task DeactivateTokenAsync(int id)
        {
            var sql = "UPDATE RefreshTokens SET Status = @Status WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, new { Id = id, Status = RefreshTokenStatus.Revoked });
            _logger.LogInformation("Refresh token {Id} has been deactivated.", id);
        }
    }
}