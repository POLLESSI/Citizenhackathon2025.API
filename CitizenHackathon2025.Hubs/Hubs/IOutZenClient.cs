using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public interface IOutZenClient
    {
        Task NewSuggestion(Suggestion dto);
    }
}
