using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICriticalAlertVoteRepository
    {
        Task InsertAsync(CriticalAlertVote vote, CancellationToken ct = default);

        Task<int> CountDistinctReportersAsync(
            CriticalAlertKind alertKind,
            string zoneKey,
            int windowMinutes,
            CancellationToken ct = default);
    }
}
