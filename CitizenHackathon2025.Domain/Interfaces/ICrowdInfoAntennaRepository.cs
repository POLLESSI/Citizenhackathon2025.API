using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdInfoAntennaRepository
    {
        Task<IReadOnlyList<CrowdInfoAntenna>> GetAllAsync(CancellationToken ct);
        Task<CrowdInfoAntenna?> GetByIdAsync(int id, CancellationToken ct);

        Task<(CrowdInfoAntenna Antenna, double DistanceMeters)?> GetNearestAsync(
            double lat, double lng, double maxRadiusMeters, CancellationToken ct);
    }
}
