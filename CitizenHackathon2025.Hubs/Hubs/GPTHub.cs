using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Hub = Microsoft.AspNetCore.SignalR.Hub;
using Microsoft.AspNetCore.Authorization;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize]
    public class GPTHub : Hub
    {
#nullable disable
        private readonly ILogger<GPTHub> _logger;
        public GPTHub(ILogger<GPTHub> logger)
        {
            _logger = logger;
        }
        public async Task RefreshGPT(string message)
        {
            _logger.LogInformation("RefreshGPT called");
            await Clients.All.SendAsync("notifynewGPT", message);
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.