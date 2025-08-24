using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using System.Threading.Tasks;

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
        Task<bool> ValidateAsync(string token);

        /// <summary>
        /// Invalidates a refresh token (revoked).
        /// </summary>
        Task InvalidateAsync(string token);

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
        Task<RefreshTokenStatus> GetStatusAsync(string token);
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.