using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Entities.ValueObjects;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface ITrafficConditionService
    {
    #nullable disable
        Task<IEnumerable<TrafficCondition?>> GetLatestTrafficConditionAsync(int limit = 10, CancellationToken ct = default);
        Task<TrafficCondition?> GetByIdAsync(int id);
        Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition @trafficCondition);
        TrafficCondition? UpdateTrafficCondition(TrafficCondition @trafficCondition);
        Task<TrafficDTO> CheckRoadAsync(Location from, string to);
        Task CheckRoadAsync(CitizenHackathon2025.Domain.ValueObjects.Location userPosition, string destination);
        Task<TrafficAnalysisResult> CheckRoadAsync(string from, string to);
        Task<int> ArchivePastTrafficConditionsAsync();
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.