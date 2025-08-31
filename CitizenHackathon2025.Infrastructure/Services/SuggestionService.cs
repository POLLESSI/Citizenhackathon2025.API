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

        public async Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync()
        {
            var suggestions = await _suggestionRepository.GetLatestSuggestionAsync();
            return suggestions;
        }
        public async Task<Suggestion?> GetByIdAsync(int id)
        {
            try
            {
                var suggestion = await _suggestionRepository.GetByIdAsync(id);
                if (suggestion == null || !suggestion.Active)
                {
                    Console.WriteLine($"Suggestion with ID {id} not found or inactive.");
                    return null;
                }
                return suggestion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving suggestion by ID {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion)
        {
            return await _suggestionRepository.SaveSuggestionAsync(suggestion);
        }
        public async Task<IEnumerable<Suggestion>> GetSuggestionsByUserAsync(int userId)
        {
            return await _suggestionRepository.GetSuggestionsByUserAsync(userId);
        }

        public async Task<bool> SoftDeleteSuggestionAsync(int id)
        {
            return await _suggestionRepository.SoftDeleteSuggestionAsync(id);
        }

        public Suggestion? UpdateSuggestion(Suggestion suggestion)
        {
            try
            {
                var updatedSuggestion = _suggestionRepository.UpdateSuggestion(suggestion);
                if (updatedSuggestion == null)
                {
                    throw new KeyNotFoundException("Suggestion not found for update.");
                }
                return updatedSuggestion;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {

                Console.WriteLine($"Validation error : {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating suggestion : {ex}");
            }
            return null;
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.