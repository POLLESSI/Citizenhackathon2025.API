using CitizenHackathon2025.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class CrowdHub : Hub
    {
        private readonly ILogger<CrowdHub> _logger;
        public CrowdHub(ILogger<CrowdHub> logger) => _logger = logger;

        public async Task RefreshCrowd(string message)
        {
            _logger.LogInformation("RefreshCrowd: {Message}", message);
            await Clients.All.SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, message);
        }

        public async Task SendCrowdUpdate(CrowdInfo crowd)
        {
            _logger.LogInformation("📡 Update Crowd sent for {Location}", crowd.LocationName);
            await Clients.All.SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, crowd);
        }
    }
}









































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.