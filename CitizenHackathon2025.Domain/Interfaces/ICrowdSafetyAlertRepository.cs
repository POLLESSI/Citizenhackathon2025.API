using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdSafetyAlertRepository
    {
        Task<CrowdSafetyAlert> InsertAsync(CrowdSafetyAlert alert, CancellationToken ct = default);

        Task<IReadOnlyList<CrowdSafetyAlert>> GetLatestAsync(int limit = 50, CancellationToken ct = default);

        Task<int?> GetBaselineConnectionsAsync(int antennaId, DateTime nowUtc, CancellationToken ct = default);

        Task<IReadOnlyList<CrowdSafetyAlert>> GetPendingRemindersAsync(int limit, CancellationToken ct);

        Task MarkReminderSentAsync(long alertId, CancellationToken ct);

        Task<bool> HasRecentSimilarAlertAsync(int antennaId, byte minSeverity, TimeSpan cooldown, CancellationToken ct = default);

        Task<int> ValidateAsync(long alertId, int validatedByUserId, CancellationToken ct = default);
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.