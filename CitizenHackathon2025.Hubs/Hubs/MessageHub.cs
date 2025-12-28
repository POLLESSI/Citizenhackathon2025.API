using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize(Policy = "User")]
    public class MessageHub : Hub
    {
        private readonly ILogger<MessageHub> _logger;

        public MessageHub(ILogger<MessageHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("[MessageHub] Connected: {Conn}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("[MessageHub] Disconnected: {Conn} ({Msg})",
                Context.ConnectionId, exception?.Message);
            return base.OnDisconnectedAsync(exception);
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.