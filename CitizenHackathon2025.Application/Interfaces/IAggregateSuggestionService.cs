using CitizenHackathon2025.Domain.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IAggregateSuggestionService
    {
        Task<string> GenerateSuggestionAsync(SuggestionContextDTO context);
    }
}
