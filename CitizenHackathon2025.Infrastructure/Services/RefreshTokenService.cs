using System.Security.Cryptography;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _repo;
        private readonly ILogger<RefreshTokenService> _logger;
        private readonly int _refreshTokenDays;

        public RefreshTokenService(
            IRefreshTokenRepository repo,
            ILogger<RefreshTokenService> logger,
            IConfiguration configuration)
        {
            _repo = repo;
            _logger = logger;

            // Default 7 days if not configured (you can add JwtSettings:RefreshTokenDays in appsettings)
            _refreshTokenDays = configuration.GetValue<int?>("JwtSettings:RefreshTokenDays") ?? 7;
        }

        public async Task<RefreshToken> GenerateAsync(string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            // Security: This user's existing active refresh tokens are revoked
            var now = DateTime.UtcNow;
            var existing = await _repo.GetByEmailAsync(email);
            foreach (var t in existing.Where(t => t.Status == RefreshTokenStatus.Active && t.ExpiryDate > now))
            {
                await _repo.UpdateStatusAsync(t.Id, RefreshTokenStatus.Revoked);
            }

            var token = GenerateSecureToken(64); // 64 bytes → ~86 chars Base64Url
            var refresh = new RefreshToken
            {
                Token = token,
                Email = email,
                ExpiryDate = now.AddDays(_refreshTokenDays),
                Status = RefreshTokenStatus.Active
            };

            await _repo.AddAsync(refresh);
            _logger.LogInformation("Refresh token created for {Email}, expires at {Expiry}", email, refresh.ExpiryDate);
            return refresh;
        }

        public async Task<bool> ValidateAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var rt = await _repo.GetByTokenAsync(token);
            return rt?.IsActive() == true;
        }

        public Task InvalidateAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));
            return _repo.RevokeAsync(token);
        }

        public Task ExpireAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));
            return _repo.ExpireAsync(token);
        }

        public Task DeactivateTokenAsync(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            return _repo.DeactivateTokenAsync(id);
        }

        public async Task<RefreshTokenStatus> GetStatusAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return RefreshTokenStatus.Expired;
            var rt = await _repo.GetByTokenAsync(token);
            return rt?.Status ?? RefreshTokenStatus.Expired;
        }

        // -------- Helpers --------
        private static string GenerateSecureToken(int sizeBytes)
        {
            var bytes = new byte[sizeBytes];
            RandomNumberGenerator.Fill(bytes);
            // Base64Url without padding
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.