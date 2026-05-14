using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdInfoRepository
    {
        Task<CrowdInfo?> UpsertCrowdInfoAsync(CrowdInfo input, CancellationToken ct = default);
        Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo);
        Task<CrowdInfoDTO> CreateManualCriticalAlertAsync(int placeId, string? reason, string? source, CancellationToken ct = default);
        Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync(int limit = 200, CancellationToken ct = default);
        Task<IEnumerable<CrowdInfo>> GetNearbyCrowdInfoAsync(double? latitude, double? longitude, int radiusKm, CancellationToken ct = default);
        Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id);
        Task<bool> DeleteCrowdInfoAsync(int id);
        Task<int> ArchivePastCrowdInfosAsync(CancellationToken ct = default);
        CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo);
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.