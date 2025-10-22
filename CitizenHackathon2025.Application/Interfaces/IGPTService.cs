using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IGPTService
    {
        Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync();
        Task<Suggestion?> GetSuggestionByIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id);
        Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetRecommendationsForSwimmingAreasAsync();
        Task SaveSuggestionAsync(Suggestion suggestion);
        Task DeleteSuggestionAsync(int id);
        Task<string> GenerateSuggestionAsync(string prompt);
        Task<int> ArchivePastGptInteractionsAsync();
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.