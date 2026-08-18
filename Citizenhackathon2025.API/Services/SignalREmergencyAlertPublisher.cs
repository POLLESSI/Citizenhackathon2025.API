using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Models;
using CitizenHackathon2025.Hubs.Services;

namespace CitizenHackathon2025.API.Services
{
    public sealed class SignalREmergencyAlertPublisher : IEmergencyAlertPublisher
    {
        private readonly EmergencyAlertHubBroadcaster _broadcaster;

        public SignalREmergencyAlertPublisher(EmergencyAlertHubBroadcaster broadcaster)
        {
            _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        }

        public Task PublishUpsertedAsync(EmergencyAlert alert, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alert);

            var dto = EmergencyAlertDtoMapper.ToSignalRDto(alert);

            return _broadcaster.PublishUpsertedAsync(dto, cancellationToken);
        }

        public Task PublishCancelledAsync(EmergencyAlert alert, CancellationToken ct = default)
        {
            return _broadcaster.PublishCancelledAsync(alert.Id, alert.SourceCode, alert.ExternalId, ct);
        }

        public Task PublishExpiredAsync(EmergencyAlert alert, CancellationToken ct = default)
        {
            return _broadcaster.PublishExpiredAsync(alert.Id, alert.SourceCode, alert.ExternalId, ct);
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.