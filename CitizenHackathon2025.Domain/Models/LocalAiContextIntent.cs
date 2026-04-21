namespace CitizenHackathon2025.Domain.Models
{
    public sealed class LocalAiContextIntent
    {
        public bool NeedPlaces { get; init; }
        public bool NeedEvents { get; init; }
        public bool NeedCrowdCalendar { get; init; }
        public bool NeedCrowdInfo { get; init; }
        public bool NeedTraffic { get; init; }
        public bool NeedWeather { get; init; }
    }
}