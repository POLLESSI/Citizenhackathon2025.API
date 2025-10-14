using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Generates a new refresh token for a user.
        /// </summary>
        Task<RefreshToken> GenerateAsync(string email);

        /// <summary>
        /// Checks that a refresh token is valid (exists, not expired, not revoked, active status).
        /// </summary>
        Task<bool> ValidateAsync(string token, string email);

        /// <summary>
        /// Invalidates a refresh token (revoked).
        /// </summary>
        Task InvalidateAsync(string token, string email);

        /// <summary>
        /// Explicitly marks a refresh token as expired.
        /// </summary>
        Task ExpireAsync(string token);

        /// <summary>
        /// Disables a refresh token (used by an admin or security process).
        /// </summary>
        Task DeactivateTokenAsync(int id);

        /// <summary>
        /// Returns the current status of a refresh token.
        /// </summary>
        Task<RefreshTokenStatus> GetStatusAsync(string token, string email);

        // 🔐 (hash/salt)
        //Task<IEnumerable<RefreshToken>> GetActiveByEmailAsync(string email);
        //Task AddHashedAsync(string email, DateTime expiryDate, byte[] tokenHash, byte[] tokenSalt);
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.