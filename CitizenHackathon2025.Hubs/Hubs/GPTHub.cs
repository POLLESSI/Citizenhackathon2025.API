using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize]
    public sealed class GPTHub : Hub<IGptClient>
    {
        private readonly ILogger<GPTHub> _logger;

        public GPTHub(ILogger<GPTHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("[GPTHub] Connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation(
                "[GPTHub] Disconnected: {ConnectionId}. Reason={Reason}",
                Context.ConnectionId,
                exception?.Message);

            await base.OnDisconnectedAsync(exception);
        }

        // Legacy only if you still need it.
        public Task RefreshGpt(string message)
        {
            _logger.LogInformation("[GPTHub] RefreshGpt called. MessageLength={Length}", message?.Length ?? 0);
            return Task.CompletedTask;
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.