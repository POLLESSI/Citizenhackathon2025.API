using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ICrowdInfoService
    {
        Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo);
        Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync();
        Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id);
        Task<bool> DeleteCrowdInfoAsync(int id);
    }
}
