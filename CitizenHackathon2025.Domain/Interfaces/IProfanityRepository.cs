using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IProfanityRepository
    {
        Task<IReadOnlyList<ProfanityWord>> GetAllActiveAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ProfanityWord>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ProfanityWord?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProfanityWord?> GetByWordAsync(string normalizedWord, CancellationToken ct = default);
        Task<ProfanityWord> InsertAsync(ProfanityWord entity, CancellationToken ct = default);
        Task<bool> UpdateAsync(ProfanityWord entity, CancellationToken ct = default);
        Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
        Task<bool> SetActiveAsync(int id, bool active, CancellationToken ct = default);
    }
}