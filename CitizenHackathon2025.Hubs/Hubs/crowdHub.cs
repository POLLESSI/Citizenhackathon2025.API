using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class CrowdHub : Hub
    {
    #nullable disable

        private readonly Microsoft.Extensions.Logging.ILogger<CrowdHub> _logger;

        public CrowdHub(ILogger<CrowdHub> logger)
        {
            _logger = logger;
        }

        public async Task RefreshCrowd(string message)
        {
            _logger.LogInformation("RefreshCrowd called");
            await Clients.All.SendAsync("notifynewCrowd", message);
        }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.