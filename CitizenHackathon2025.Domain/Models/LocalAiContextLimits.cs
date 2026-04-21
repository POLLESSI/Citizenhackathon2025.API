namespace CitizenHackathon2025.Domain.Models
{
    public sealed class LocalAiContextLimits
    {
        public int MaxPlaces { get; init; } = 3;
        public int MaxEvents { get; init; } = 2;
        public int MaxCrowdCalendar { get; init; } = 2;
        public int MaxCrowdInfo { get; init; } = 1;
        public int MaxTraffic { get; init; } = 1;
        public int MaxWeather { get; init; } = 1;
        public double RadiusKm { get; init; } = 20.0;
    }
}