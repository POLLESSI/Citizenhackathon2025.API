using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class SuggestionHub : Hub
    {
    #nullable disable

        private readonly ILogger<SuggestionHub> _logger;
        public SuggestionHub(ILogger<SuggestionHub> logger) => _logger = logger;

        // Client -> Server: simple ping broadcast without payload
        public async Task RefreshSuggestion()
        {
            _logger.LogInformation("NotifyNewSuggestion called");
            await Clients.All.SendAsync(SuggestionHubMethods.ToClient.NewSuggestion);
        }

        // Client -> Server: sending a Suggestion to other clients
        public async Task SendSuggestion(SuggestionDTO suggestion)
        {
            _logger.LogInformation("Sending Suggestion to clients: {@Suggestion}", suggestion);
            await Clients.All.SendAsync(SuggestionHubMethods.ToClient.ReceiveSuggestion, suggestion);
        }
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.