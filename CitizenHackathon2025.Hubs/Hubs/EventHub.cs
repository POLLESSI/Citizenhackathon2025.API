using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;

namespace CitizenHackathon2025.Hubs.Hubs
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
            await Clients.All.SendAsync(EventHubMethods.ToClient.NewEvent, message);
        }
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.