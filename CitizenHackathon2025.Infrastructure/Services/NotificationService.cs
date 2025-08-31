using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }


        /// <summary>
        /// Send a simple notification (console, log, etc.)
        /// </summary>
        /// <param name="message">The message to be notified</param>
        public async Task NotifyAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("The notification message cannot be empty.");

            _logger.LogInformation("📢 Notification : {Message}", message);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
        }

        /// <summary>
        /// Sends a notification related to a specific entity or event
        /// </summary>
        /// <param name="type">Type of entity or event</param>
        /// <param name="message">Related post</param>
        public async Task NotifyEventAsync(string type, string message)
        {
            string formatted = $"[{type}] {message}";
            _logger.LogInformation("📣 [{Type}] : {Message}", type, message);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", formatted);
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.