using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByIdAsync(int id);
        Task<RefreshToken?> GetByTokenAsync(string token); // (legacy, may be deleted)
        Task<IEnumerable<RefreshToken>> GetByEmailAsync(string email);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateStatusAsync(int id, RefreshTokenStatus status);
        Task RevokeAsync(string token);
        Task ExpireAsync(string token);
        Task DeactivateTokenAsync(int id);
        // 🔐 (hash/salt)
        Task<IEnumerable<RefreshToken>> GetActiveByEmailAsync(string email);
        Task AddHashedAsync(string email, DateTime expiryDate, byte[] tokenHash, byte[] tokenSalt);
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.