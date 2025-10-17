using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Domain.DTOs
{
    // Domain
    public class CrowdCalendarEntry
    {
        public int Id { get; set; }
        public DateTime DateUtc { get; set; }
        public string RegionCode { get; set; } = "";
        public int? PlaceId { get; set; }
        public string? EventName { get; set; }
        public CrowdLevelEnum ExpectedLevel { get; set; }
        public byte? Confidence { get; set; }
        public TimeSpan? StartLocalTime { get; set; }
        public TimeSpan? EndLocalTime { get; set; }
        public int LeadHours { get; set; } = 3;
        public string? MessageTemplate { get; set; }
        public string? Tags { get; set; }
        public bool Active { get; set; } = true;
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.