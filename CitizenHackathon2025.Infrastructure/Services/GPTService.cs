using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class GPTService : IGPTService
    {
        private readonly IGPTRepository _gptRepository;
        //private readonly IHubContext<SuggestionHub> _hubContext;
        private readonly ILogger<GPTService> _logger;

        public GPTService(IGPTRepository gptRepository, ILogger<GPTService> logger)
        {
            _gptRepository = gptRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            return await _gptRepository.GetAllSuggestionsAsync();
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int eventId)
        {
            return await _gptRepository.GetSuggestionsByEventIdAsync(eventId);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int forecastId)
        {
            return await _gptRepository.GetSuggestionsByForecastIdAsync(forecastId);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int trafficId)
        {
            return await _gptRepository.GetSuggestionsByTrafficIdAsync(trafficId);
        }

        public async Task SaveSuggestionAsync(Suggestion suggestion)
        {
            await _gptRepository.SaveSuggestionAsync(suggestion);
            //await _hubContext.Clients.All.SendAsync("SuggestionAdded", suggestion);
            _logger.LogInformation("Suggestion enregistrée et envoyée via SignalR : {@Suggestion}", suggestion);
        }

        public async Task DeleteSuggestionAsync(int suggestionId)
        {
            await _gptRepository.DeleteSuggestionAsync(suggestionId);
            //await _hubContext.Clients.All.SendAsync("SuggestionDeleted", suggestionId);
            _logger.LogInformation("Suggestion supprimée et signalée via SignalR : Id={Id}", suggestionId);
        }
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.