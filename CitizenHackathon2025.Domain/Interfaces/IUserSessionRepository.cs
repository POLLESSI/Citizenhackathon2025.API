using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Queries;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IUserSessionRepository
    {
        Task UpsertAsync(UserSession s);
        Task TouchAsync(string jti, DateTime nowUtc);
        Task<bool> IsRevokedAsync(string jti);
        Task<int> RevokeAsync(string jti, string reason);
        Task<IEnumerable<UserSession>> QueryAsync(SessionQuery q);
        Task<int> PurgeExpiredAsync();
    }
}
