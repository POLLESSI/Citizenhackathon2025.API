namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class LocalAiIntent
    {
        public bool NeedEvents { get; init; }
        public bool NeedCrowdCalendar { get; init; }
        public bool NeedCrowdInfo { get; init; }
        public bool NeedTraffic { get; init; }
        public bool NeedWeather { get; init; }
    }
}
