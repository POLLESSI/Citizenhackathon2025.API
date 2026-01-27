using CitizenHackathon2025.DTOs.DTOs.Antennas;

namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public interface IAntennaSnapshotReadRepository
    {
        Task<IReadOnlyList<AntennaSnapshotDTO>> GetLatestPerAntennaAsync(
            short windowSeconds,
            int lookbackSeconds,
            CancellationToken ct);
    }
}
