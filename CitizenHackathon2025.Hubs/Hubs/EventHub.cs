using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class EventHub : Hub
    {
#nullable disable

        private readonly ILogger<EventHub> _logger;

        public EventHub(ILogger<EventHub> logger)
        {
            _logger = logger;
        }

        public async Task RefreshEvent(string message)
        {
            _logger.LogInformation("NotifyNewEvent called");
            await Clients.All.SendAsync("NewEvent", message);
        }
    }
}
