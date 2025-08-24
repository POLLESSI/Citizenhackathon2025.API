using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByIdAsync(int id);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> GetByEmailAsync(string email);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateStatusAsync(int id, RefreshTokenStatus status);
        Task RevokeAsync(string token);
        Task ExpireAsync(string token);
        Task DeactivateTokenAsync(int id);
    }
}
