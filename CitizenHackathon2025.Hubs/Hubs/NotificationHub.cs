using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Hubs
{
    /// <summary>
    /// Generic notification hub (system, user, broadcast).
    /// Use NotificationHubMethods for names/paths.
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>Test ping from the client.</summary>
        public Task Ping(string message = "ping")
        {
            _logger.LogInformation("[NotificationHub] Ping: {Message}", message);
            return Clients.Caller.SendAsync(NotificationHubMethods.ToClient.Notify, $"pong: {message}");
        }

        /// <summary>Broadcast a message to all clients.</summary>
        public Task Broadcast(string message)
        {
            _logger.LogInformation("[NotificationHub] Broadcast: {Message}", message);
            return Clients.All.SendAsync(NotificationHubMethods.ToClient.Notify, message);
        }

        /// <summary>Targeted notification of a user (by groupId = email/userId, to be harmonized with your auth).</summary>
        public Task NotifyUser(string userIdOrEmail, string message)
        {
            _logger.LogInformation("[NotificationHub] NotifyUser: {User} -> {Message}", userIdOrEmail, message);
            // Convention: each user joins a group named by their identifier
            return Clients.Group(userIdOrEmail).SendAsync(NotificationHubMethods.ToClient.NotifyUser, userIdOrEmail, message);
        }

        /// <summary>Join a group (convention: userId/email, or functional group).</summary>
        public Task JoinGroup(string group)
        {
            if (!string.IsNullOrWhiteSpace(group))
            {
                _logger.LogInformation("[NotificationHub] {Conn} joined {Group}", Context.ConnectionId, group);
                return Groups.AddToGroupAsync(Context.ConnectionId, group);
            }
            return Task.CompletedTask;
        }

        /// <summary>Leave a group.</summary>
        public Task LeaveGroup(string group)
        {
            if (!string.IsNullOrWhiteSpace(group))
            {
                _logger.LogInformation("[NotificationHub] {Conn} left {Group}", Context.ConnectionId, group);
                return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            }
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("[NotificationHub] Connected: {Conn}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation("[NotificationHub] Disconnected: {Conn} ({Err})", Context.ConnectionId, exception?.Message);
            return base.OnDisconnectedAsync(exception);
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.