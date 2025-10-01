using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class TrafficHub : Hub
    {
        private readonly ILogger<TrafficHub> _logger;
        public TrafficHub(ILogger<TrafficHub> logger) => _logger = logger;

        public async Task RefreshTraffic()
        {
            _logger.LogInformation("RefreshTraffic called");
            await Clients.All.SendAsync(TrafficConditionHubMethods.ToClient.NotifyNewTraffic);
        }
    }
}











































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.