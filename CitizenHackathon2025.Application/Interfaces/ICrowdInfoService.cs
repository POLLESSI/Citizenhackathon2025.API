using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface ICrowdInfoService
    {
        Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo, CancellationToken ct = default);
        Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync(CancellationToken ct = default);
        Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id, CancellationToken ct = default);
        Task<bool> DeleteCrowdInfoAsync(int id, CancellationToken ct = default);
        CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo); // sync → pas de token
        Task<CrowdLevelDTO> GetCrowdLevelAsync(string destination, CancellationToken ct = default);
    }
}












































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.