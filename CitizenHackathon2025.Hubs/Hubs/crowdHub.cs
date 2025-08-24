using CitizenHackathon2025.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class CrowdHub : Hub
    {
        private readonly ILogger<CrowdHub> _logger;

        public CrowdHub(ILogger<CrowdHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Notifies all customers that the map needs to be refreshed (generic case).
        /// </summary>
        public async Task RefreshCrowd(string message)
        {
            _logger.LogInformation("📢 RefreshCrowd called : {Message}", message);
            await Clients.All.SendAsync("notifyNewCrowd", message);
        }

        /// <summary>
        /// Sends a full CrowdInfo update to all connected clients.
        /// </summary>
        public async Task SendCrowdUpdate(CrowdInfo crowd)
        {
            _logger.LogInformation("📡 Update Crowd sent for {Location}", crowd.LocationName);
            await Clients.All.SendAsync("ReceiveCrowdUpdate", crowd);
        }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.