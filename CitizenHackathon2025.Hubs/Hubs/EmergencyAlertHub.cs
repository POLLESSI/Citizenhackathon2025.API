using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public sealed class EmergencyAlertHub : Hub<IEmergencyAlertHubClient>
    {
        private readonly ILogger<EmergencyAlertHub> _logger;

        public EmergencyAlertHub(ILogger<EmergencyAlertHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Emergency alert SignalR client connected. " + "ConnectionId={ConnectionId}", Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception is null)
            {
                _logger.LogInformation("Emergency alert SignalR client disconnected. " + "ConnectionId={ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning(exception, "Emergency alert SignalR client disconnected " + "with an error. ConnectionId={ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task SubscribeAll()
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, EmergencyAlertHubMethods.AllGroup);
        }

        public Task UnsubscribeAll()
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, EmergencyAlertHubMethods.AllGroup);
        }

        public Task SubscribeProvince(string provinceCode)
        {
            var group = EmergencyAlertHubMethods.ProvinceGroup(provinceCode);

            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task UnsubscribeProvince(string provinceCode)
        {
            var group = EmergencyAlertHubMethods.ProvinceGroup(provinceCode);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        public Task SubscribeMunicipality(string municipalityCode)
        {
            var group = EmergencyAlertHubMethods.MunicipalityGroup(municipalityCode);

            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task UnsubscribeMunicipality(string municipalityCode)
        {
            var group = EmergencyAlertHubMethods.MunicipalityGroup(municipalityCode);

            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.