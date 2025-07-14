using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ICrowdInfoService
    {
        Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo);
        Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync();
        Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id);
        Task<bool> DeleteCrowdInfoAsync(int id);
        CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo);
        Task<CrowdLevelDTO> GetCrowdLevelAsync(string destination);
    }
}












































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.