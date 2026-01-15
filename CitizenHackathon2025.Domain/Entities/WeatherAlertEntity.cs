namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class WeatherAlertEntity
    {
        public int Id { get; set; }
        public string Provider { get; set; } = "openweather";
        public string ExternalId { get; set; } = "";

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? SenderName { get; set; }
        public string? EventName { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public byte? Severity { get; set; }
        public DateTime LastSeenAt { get; set; }
        public bool Active { get; set; }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.