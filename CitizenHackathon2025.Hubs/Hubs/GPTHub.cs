using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

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
            await Clients.All.SendAsync(GptInteractionHubMethods.ToClient.NotifyNewGpt, message);
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.