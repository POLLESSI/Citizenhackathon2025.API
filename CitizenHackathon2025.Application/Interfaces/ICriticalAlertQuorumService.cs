using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface ICriticalAlertQuorumService
    {
        Task<CriticalAlertQuorumResult> RegisterVoteAsync(
            CriticalAlertKind kind,
            int? placeId,
            decimal latitude,
            decimal longitude,
            string? deviceId,
            string? reason,
            CancellationToken ct = default);
    }
}
