namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiCrowdInfoContextDTO
    {
        public int Id { get; set; }

        public string? LocationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int? CrowdLevel { get; set; }
        public DateTime? Timestamp { get; set; }

        public double? DistanceKm { get; set; }
        public bool Active { get; set; }
    }
}