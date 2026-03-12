using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IMistralAIService
    {
        //Task<GPTInteraction?> UpsertInteractionAsync(GPTInteraction interaction);
        Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync(string prompt, CancellationToken ct = default);
        Task<Suggestion?> GetSuggestionByIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetWeatherAdvisoryAsync(string location, CancellationToken ct = default);
        Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetRecommendationsForSwimmingAreasAsync();
        Task<Suggestion?> SaveSuggestionAsync(Suggestion suggestion, CancellationToken ct = default);
        Task DeleteSuggestionAsync(int id);
        Task<string> GenerateSuggestionAsync(string prompt, double? latitude = null, double? longitude = null, CancellationToken ct = default);
        Task<int> ArchivePastGptInteractionsAsync();
    }
}
