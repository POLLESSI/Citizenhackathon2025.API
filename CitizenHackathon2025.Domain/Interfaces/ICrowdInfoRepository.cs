using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface ICrowdInfoRepository
    {
        Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo);
        Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync();
        Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id);
        Task<bool> DeleteCrowdInfoAsync(int id);
        CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo);
    }
}












































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.