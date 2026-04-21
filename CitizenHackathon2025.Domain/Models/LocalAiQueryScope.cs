namespace CitizenHackathon2025.Domain.Models
{
    public sealed class LocalAiQueryScope
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public DateTime TargetDate { get; init; }
        public double RadiusKm { get; init; }
    }
}