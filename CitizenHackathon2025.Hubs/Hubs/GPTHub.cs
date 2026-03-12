using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
        public async Task NotifyNewGpt(GptInteractionDTO dto)
        {
            await Clients.All.SendAsync("ReceiveGptResponse", dto);
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.