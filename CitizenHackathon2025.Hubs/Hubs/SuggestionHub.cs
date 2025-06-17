using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.