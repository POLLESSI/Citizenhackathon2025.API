using CitizenHackathon2025.Domain.Common;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IProfanityRepository
    {
        Task<IReadOnlyList<ProfanityWord>> GetAllActiveAsync(CancellationToken ct = default);
        Task<PagedResultDto<ProfanityWord>> GetPagedAsync(int page, int pageSize, string? languageCode = null, string? search = null, CancellationToken ct = default);
        Task<ProfanityWord?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProfanityWord?> GetByWordAsync(string normalizedWord, CancellationToken ct = default);
        Task<ProfanityWord> InsertAsync(ProfanityWord entity, CancellationToken ct = default);
        Task<bool> UpdateAsync(ProfanityWord entity, CancellationToken ct = default);
        Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
        Task<bool> SetActiveAsync(int id, bool active, CancellationToken ct = default);
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.