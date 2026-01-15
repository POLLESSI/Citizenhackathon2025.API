using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Hubs.Services
{
    public interface ICrowdInfoAntennaService
    {
        Task<IReadOnlyList<CrowdInfoAntennaDTO>> GetAllAsync(CancellationToken ct);
        Task<CrowdInfoAntennaDTO?> GetByIdAsync(int id, CancellationToken ct);

        Task<NearestAntennaDTO?> GetNearestAsync(double lat, double lng, double maxRadiusMeters, CancellationToken ct);

        Task<AntennaCountsDTO> GetCountsAsync(int antennaId, int windowMinutes, CancellationToken ct);

        // Use-case “événement -> antenne la plus proche -> counts”
        Task<EventAntennaCrowdDTO?> GetEventCrowdAsync(int eventId, int windowMinutes, double maxRadiusMeters, CancellationToken ct);
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.