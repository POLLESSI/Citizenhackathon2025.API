using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public interface IOutZenClient
    {
        Task NewSuggestion(Suggestion dto);
        Task CrowdInfoUpdated(object dto);
        Task SuggestionsUpdated(IEnumerable<object> suggestions);
        Task WeatherUpdated(object forecast);
        Task TrafficUpdated(object traffic);
    }
}
