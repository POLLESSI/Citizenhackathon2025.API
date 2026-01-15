using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize]
    public sealed class CrowdInfoAntennaHub : Hub
    {
        private readonly ILogger<CrowdInfoAntennaHub> _logger;

        public CrowdInfoAntennaHub(ILogger<CrowdInfoAntennaHub> logger)
            => _logger = logger;

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("CrowdInfoAntennaHub connected: {ConnId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation("CrowdInfoAntennaHub disconnected: {ConnId}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // Client subscription (optional, if you want per-antenna streams)
        public Task JoinAntenna(int antennaId)
        {
            var group = CrowdInfoAntennaHubMethods.AntennaGroup(antennaId);
            _logger.LogDebug("JoinAntenna {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task LeaveAntenna(int antennaId)
        {
            var group = CrowdInfoAntennaHubMethods.AntennaGroup(antennaId);
            _logger.LogDebug("LeaveAntenna {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.