namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class CrowdAlertVote
    {
        public int Id { get; set; }
        public int PlaceId { get; set; }
        public string ZoneKey { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public string? DeviceHash { get; set; }
        public string? IpHash { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public string? Reason { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true;
    }
}












































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.