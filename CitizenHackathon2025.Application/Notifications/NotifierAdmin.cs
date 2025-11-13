using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Shared.Notifications
{
    public class NotifierAdmin : INotifierAdmin
    {
        private readonly ILogger<NotifierAdmin> _logger;
        public NotifierAdmin(ILogger<NotifierAdmin> logger) => _logger = logger;

        public Task NotifyAdminAsync(object message)
        {
            _logger.LogWarning("ADMIN ALERT => {@Message}", message);
            // TODO : send via SignalR, email, Slack, etc.
            return Task.CompletedTask;
        }
    }
}



















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.