namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiCriticalAlertContextDTO
    {
        public string AlertKind { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? PlaceName { get; set; }
        public string? Description { get; set; }
        public int Severity { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public double? DistanceKm { get; set; }
    }
}
