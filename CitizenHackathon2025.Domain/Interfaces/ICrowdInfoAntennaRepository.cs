using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdInfoAntennaRepository
    {
        Task<IReadOnlyList<CrowdInfoAntenna>> GetAllAsync(CancellationToken ct);
        Task<CrowdInfoAntenna?> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<CrowdInfoAntenna>> GetActiveAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CrowdInfoAntenna>> GetByBoundsAsync(double minLat, double maxLat, double minLng, double maxLng, CancellationToken ct = default);
        Task<(CrowdInfoAntenna Antenna, double DistanceMeters)?> GetNearestAsync(
            double lat, double lng, double maxRadiusMeters, CancellationToken ct);
        Task<CrowdInfoAntenna> CreateAntennaAsync(CrowdInfoAntenna antenna, CancellationToken ct);
        Task<bool> DeleteAntennaAsync(int id, CancellationToken ct);
        Task<CrowdInfoAntenna> UpsertFromCadastreAsync(CrowdInfoAntenna antenna, CancellationToken ct);
    }
}
 






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.