using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Services
{
    public sealed class EmergencyAlertHubBroadcaster
    {
        private readonly IHubContext<EmergencyAlertHub, IEmergencyAlertHubClient> _hubContext;

        public EmergencyAlertHubBroadcaster(IHubContext<EmergencyAlertHub, IEmergencyAlertHubClient> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext)); ;
        }

        public async Task PublishUpsertedAsync(EmergencyAlertSignalRDTO alert, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alert);

            cancellationToken.ThrowIfCancellationRequested();

            await _hubContext.Clients.Group(EmergencyAlertHubMethods.AllGroup).EmergencyAlertUpserted(alert);

            if (!string.IsNullOrWhiteSpace(alert.ProvinceCode))
            {
                await _hubContext.Clients.Group(EmergencyAlertHubMethods.ProvinceGroup(alert.ProvinceCode)).EmergencyAlertUpserted(alert);
            }

            if (!string.IsNullOrWhiteSpace(alert.MunicipalityCode))
            {
                await _hubContext.Clients.Group(EmergencyAlertHubMethods.MunicipalityGroup(alert.MunicipalityCode)).EmergencyAlertUpserted(alert);
            }
                            
        }

        public Task PublishCancelledAsync(Guid alertId, string sourceCode, string externalId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _hubContext.Clients.Group(EmergencyAlertHubMethods.AllGroup).EmergencyAlertCancelled(alertId, sourceCode, externalId);
        }

        public Task PublishExpiredAsync(Guid alertId, string sourceCode, string externalId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _hubContext.Clients.Group(EmergencyAlertHubMethods.AllGroup).EmergencyAlertExpired(alertId,sourceCode, externalId);
        }

        public Task PublishRefreshAsync(EmergencyAlertRefreshDTO refresh, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _hubContext.Clients.Group(EmergencyAlertHubMethods.AllGroup).EmergencyAlertsRefreshed(refresh);
        }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.