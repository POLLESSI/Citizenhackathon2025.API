using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class SuggestionService : ISuggestionService
    {
        private readonly ISuggestionRepository _suggestionRepository;

        public SuggestionService(ISuggestionRepository suggestionRepository)
        {
            _suggestionRepository = suggestionRepository;
        }

        public async Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync(CancellationToken ct = default)
            => await _suggestionRepository.GetLatestSuggestionAsync();
        public Task<IEnumerable<Suggestion?>> GetAllSuggestionsAsync(int limit = 100, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
        public async Task<Suggestion?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var suggestion = await _suggestionRepository.GetByIdAsync(id);
                if (suggestion == null || !suggestion.Active) return null;
                return suggestion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving suggestion by ID {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion, CancellationToken ct = default)
            => await _suggestionRepository.SaveSuggestionAsync(suggestion);

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByUserAsync(int userId, CancellationToken ct = default)
            => await _suggestionRepository.GetSuggestionsByUserAsync(userId);

        public async Task<bool> SoftDeleteSuggestionAsync(int id, CancellationToken ct = default)
            => await _suggestionRepository.SoftDeleteSuggestionAsync(id);

        public Suggestion? UpdateSuggestion(Suggestion suggestion)
            => _suggestionRepository.UpdateSuggestion(suggestion);

        
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.