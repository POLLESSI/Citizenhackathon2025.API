using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class SuggestionHub : Hub
    {
#nullable disable

        private readonly ILogger<SuggestionHub> _logger;

        public SuggestionHub(ILogger<SuggestionHub> logger)
        {
            _logger = logger;
        }

        public async Task RefreshSuggestion()
        {
            _logger.LogInformation("NotifyNewSuggestion called");
            await Clients.All.SendAsync("NewSuggestion");
        }
        public async Task SendSuggestion(SuggestionDTO suggestion)
        {
            _logger.LogInformation("Sending Suggestion to clients: {@Suggestion}", suggestion);
            await Clients.All.SendAsync("ReceiveSuggestion", suggestion);
        }
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.