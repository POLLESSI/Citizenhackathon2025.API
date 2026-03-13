namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiCrowdCalendarContextDTO
    {
        public int Id { get; set; }

        public DateTime? DateUtc { get; set; }
        public string? RegionCode { get; set; }
        public int? PlaceId { get; set; }

        public string? EventName { get; set; }
        public int? ExpectedLevel { get; set; }
        public int? Confidence { get; set; }

        public TimeSpan? StartLocalTime { get; set; }
        public TimeSpan? EndLocalTime { get; set; }
        public int? LeadHours { get; set; }

        public string? MessageTemplate { get; set; }
        public string? Tags { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }

        public bool Active { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}