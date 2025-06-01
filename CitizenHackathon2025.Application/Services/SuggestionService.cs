using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Application.Services
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

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion)
        {
            return await _suggestionRepository.SaveSuggestionAsync(suggestion);
        }
    }
}
