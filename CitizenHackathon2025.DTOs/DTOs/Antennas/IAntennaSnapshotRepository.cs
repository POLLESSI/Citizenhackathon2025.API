using CitizenHackathon2025.DTOs.DTOs.Antennas;

namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public interface IAntennaSnapshotRepository
    {
        Task BulkUpsertAsync(IEnumerable<IAntennaTelemetryAggregator.AntennaSnapshotRow> rows, CancellationToken ct);
    }
}
