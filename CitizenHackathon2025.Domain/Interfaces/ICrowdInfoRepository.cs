using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdInfoRepository
    {
        Task<CrowdInfo?> UpsertCrowdInfoAsync(CrowdInfo input, CancellationToken ct = default);
        Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo);
        Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync(int limit = 200, CancellationToken ct = default);
        Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id);
        Task<bool> DeleteCrowdInfoAsync(int id);
        Task<int> ArchivePastCrowdInfosAsync();
        CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo);
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.