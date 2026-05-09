using CitizenHackathon2025.DTOs.DTOs.Antennas;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IAntennaSimulationService
    {
        Task SimulateAsync(
            SimulateAntennaConnectionsRequest request,
            CancellationToken ct = default);
    }
}
