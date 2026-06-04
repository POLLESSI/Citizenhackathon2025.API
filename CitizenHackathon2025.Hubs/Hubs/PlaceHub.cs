using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class PlaceHub : Hub
    {
        private readonly ILogger<PlaceHub> _logger;

        public PlaceHub(ILogger<PlaceHub> logger)
        {
            _logger = logger;
        }

        public async Task RefreshPlace(string message)
        {
            _logger.LogInformation("RefreshPlace called");
            await Clients.All.SendAsync(PlaceHubMethods.ToClient.NewPlace, message);
        }

        public async Task DeclareFullAlert(FullAlertDTO alert)
        {
            alert.DeclaredAtUtc = DateTime.UtcNow;
            alert.ExpiresAtUtc = alert.DeclaredAtUtc.AddMinutes(5);

            await Clients.All.SendAsync("FullAlertDeclared", alert);
        }
    }
}







































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.