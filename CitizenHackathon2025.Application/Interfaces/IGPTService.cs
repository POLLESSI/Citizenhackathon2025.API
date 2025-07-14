using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Application.Interfaces
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
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.