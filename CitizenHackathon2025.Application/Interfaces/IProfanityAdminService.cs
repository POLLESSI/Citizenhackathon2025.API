using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IProfanityAdminService
    {
        Task<IReadOnlyList<ProfanityWord>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ProfanityWord?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProfanityWord> CreateAsync(ProfanityWord entity, CancellationToken ct = default);
        Task<bool> UpdateAsync(ProfanityWord entity, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
        Task<bool> SetActiveAsync(int id, bool active, CancellationToken ct = default);
    }
}