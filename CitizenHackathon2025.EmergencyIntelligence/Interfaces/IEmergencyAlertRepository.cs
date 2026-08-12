using CitizenHackathon2025.EmergencyIntelligence.Models;

namespace CitizenHackathon2025.EmergencyIntelligence.Interfaces
{
    public enum EmergencyAlertRemovalReason
    {
        Superseded,
        Cancelled,
        Expired
    }

    public sealed record EmergencyAlertRemoval(EmergencyAlert Alert, EmergencyAlertRemovalReason Reason);

    public sealed record EmergencyAlertApplyResult(EmergencyAlert StoredAlert, bool Changed, bool IsActive, IReadOnlyList<EmergencyAlertRemoval> RemovedAlerts);

    public interface IEmergencyAlertRepository
    {
        Task<EmergencyAlertApplyResult> ApplyAsync(EmergencyAlert alert, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmergencyAlert>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmergencyAlert>> ExpireDueAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
    }
}