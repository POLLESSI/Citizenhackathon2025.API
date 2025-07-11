using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.GPTInteraction;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IGPTService
    {
        Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync();
        Task<Suggestion?> GetSuggestionByIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id);
        Task SaveSuggestionAsync(Suggestion suggestion);
        Task DeleteSuggestionAsync(int id);
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.