using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<string> GenerateAsync(string email);
        Task<bool> ValidateAsync(string token);
        Task InvalidateAsync(string token);
        Task<IEnumerable<RefreshToken>> GetTokensForUserAsync(string email);
    }
}
