using CitizenHackathon2025.DTOs.DTOs.Antennas;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IAntennaZoneSimulationService
    {
        Task SimulateAsync(
            SimulateAntennaZoneRequest request,
            CancellationToken ct = default);
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.