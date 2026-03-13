namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiTrafficContextDTO
    {
        public int Id { get; set; }

        public DateTime? DateCondition { get; set; }
        public string? CongestionLevel { get; set; }
        public string? IncidentType { get; set; }

        public string? Provider { get; set; }
        public string? ExternalId { get; set; }

        public string? Title { get; set; }
        public string? Road { get; set; }
        public int? Severity { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }

        public bool Active { get; set; }
    }
}