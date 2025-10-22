using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Repositories;
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

        public Task<Suggestion?> GetSuggestionByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetRecommendationsForSwimmingAreasAsync()
        {
            DateTime fromLast24h = DateTime.UtcNow.AddHours(-24);
            return await _gptRepository.GetSuggestionsGroupedByPlaceAsync("Swimming area", indoorFilter: false, sinceDate: fromLast24h);
        }

        public Task<string> GenerateSuggestionAsync(string prompt)
        {
            throw new NotImplementedException();
        }

        public async Task<int> ArchivePastGptInteractionsAsync()
        {
            string sql = "UPDATE GptInteraction SET Active = 0 WHERE CreatedAt < @Threshold AND Active = 1";
            var parameters = new { Threshold = DateTime.UtcNow.Date.AddDays(-2) };
            return await _gptRepository.ArchivePastGptInteractionsAsync();
        }
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.