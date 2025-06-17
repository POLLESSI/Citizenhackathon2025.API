using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Hubs.Hubs
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
            await Clients.All.SendAsync("Newplace", message);
        }
    }
}







































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.