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
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.