namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class DisasterAlert
    {
        public long Id { get; set; }
        public byte DisasterType { get; set; }
        public byte Severity { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? PlaceName { get; set; }
        public string? Description { get; set; }
        public int ConfirmationCount { get; set; }
        public int RequiredCount { get; set; }
        public string Status { get; set; } = "Confirmed";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public bool Active { get; set; } = true;
    }
}





































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.