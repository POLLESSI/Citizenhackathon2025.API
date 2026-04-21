namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiPlaceContextDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public bool? Indoor { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int? Capacity { get; set; }
        public string? Tag { get; set; }

        public string? ExternalSource { get; set; }
        public string? ExternalId { get; set; }
        public DateTime? SourceUpdatedAtUtc { get; set; }

        public double? DistanceKm { get; set; }
        public bool Active { get; set; }
    }
}
