using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdAlertVoteRepository
    {
        Task InsertAsync(CrowdAlertVote vote, CancellationToken ct = default);

        Task<int> CountDistinctReportersAsync(
            string zoneKey,
            int windowMinutes,
            CancellationToken ct = default);
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.