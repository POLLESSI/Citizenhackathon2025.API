using CitizenHackathon2025.Domain.Models;
using CitizenHackathon2025.Domain.ValueObjects;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ITrafficProvider
    {
        string Name { get; }
        Task<IReadOnlyList<TrafficEvent>> GetIncidentsAsync(BoundingBox bbox, CancellationToken ct);
        Task<IReadOnlyList<TrafficFlowSegment>> GetFlowAsync(BoundingBox bbox, CancellationToken ct); // optionnel
    }
}
