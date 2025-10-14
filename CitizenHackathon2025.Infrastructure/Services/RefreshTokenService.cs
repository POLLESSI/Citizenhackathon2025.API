using System.Security.Cryptography;
using System.Text;
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

            // revokes existing assets
            var now = DateTime.UtcNow;
            var existing = await _repo.GetByEmailAsync(email);
            foreach (var t in existing.Where(t => t.Status == RefreshTokenStatus.Active && t.ExpiryDate > now))
                await _repo.UpdateStatusAsync(t.Id, RefreshTokenStatus.Revoked);

            // generate token + (salt, hash) for the DB
            var (token, salt, hash) = Create();

            var refresh = new RefreshToken
            {
                // ⚠ we return `Token` to the client,
                //   but we only store Hash/Salt in DB
                Token = token,
                Email = email,
                ExpiryDate = now.AddDays(_refreshTokenDays),
                Status = RefreshTokenStatus.Active,

                // ↓ new fields (add them to the template)
                TokenSalt = salt,
                TokenHash = hash
            };

            await _repo.AddAsync(refresh); // impl: INSERT TokenHash/TokenSalt, (optional: do not insert Token anymore)
            _logger.LogInformation("Refresh token created for {Email}, expires at {Expiry}", email, refresh.ExpiryDate);

            // we can return an object that does NOT contain Salt/Hash
            return refresh;
        }

        public async Task<bool> ValidateAsync(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return false;

            var candidates = await _repo.GetActiveByEmailAsync(email);
            foreach (var rt in candidates)
            {
                var recomputed = SHA256.HashData(Combine(Encoding.UTF8.GetBytes(token), rt.TokenSalt));
                if (CryptographicOperations.FixedTimeEquals(recomputed, rt.TokenHash))
                    return rt.IsActive();
            }
            return false;
        }
        public Task InvalidateAsync(string token)
        {
            throw new NotImplementedException();
        }
        public async Task InvalidateAsync(string token, string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            var candidates = await _repo.GetActiveByEmailAsync(email);
            foreach (var rt in candidates)
            {
                var recomputed = SHA256.HashData(Combine(Encoding.UTF8.GetBytes(token), rt.TokenSalt));
                if (CryptographicOperations.FixedTimeEquals(recomputed, rt.TokenHash))
                {
                    await _repo.UpdateStatusAsync(rt.Id, RefreshTokenStatus.Revoked);
                    return;
                }
            }
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
        public Task<RefreshTokenStatus> GetStatusAsync(string token)
        {
            throw new NotImplementedException();
        }
        public async Task<RefreshTokenStatus> GetStatusAsync(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token)) return RefreshTokenStatus.Expired;

            var candidates = await _repo.GetActiveByEmailAsync(email); // only Active & not expired
            foreach (var rt in candidates)
            {
                var recomputed = SHA256.HashData(Combine(Encoding.UTF8.GetBytes(token), rt.TokenSalt));
                if (CryptographicOperations.FixedTimeEquals(recomputed, rt.TokenHash))
                    return rt.Status;
            }
            return RefreshTokenStatus.Expired;
        }

        // -------- Helpers --------
        //private static string GenerateSecureToken(int sizeBytes)
        //{
        //    var bytes = new byte[sizeBytes];
        //    RandomNumberGenerator.Fill(bytes);
        //    // Base64Url without padding
        //    return Convert.ToBase64String(bytes)
        //        .Replace('+', '-')
        //        .Replace('/', '_')
        //        .TrimEnd('=');
        //}
        private static (string token, byte[] salt, byte[] hash) Create()
        {
            var token = Base64Url(RandomNumberGenerator.GetBytes(32));  // ~43 chars
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = SHA256.HashData(Combine(Encoding.UTF8.GetBytes(token), salt));
            return (token, salt, hash);
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');      
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.