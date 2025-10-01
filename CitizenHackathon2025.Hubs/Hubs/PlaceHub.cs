using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class PlaceHub : Hub
    {
#nullable disable

        private readonly ILogger<PlaceHub> _logger;

        public PlaceHub(ILogger<PlaceHub> logger)
        {
            _logger = logger;
        }

        public async Task RefreshPlace(string message)
        {
            _logger.LogInformation("NotifyNewPlace called");
            await Clients.All.SendAsync(PlaceHubMethods.ToClient.NewPlace, message);
        }
    }
}







































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.