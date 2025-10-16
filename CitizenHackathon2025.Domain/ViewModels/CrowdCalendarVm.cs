using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Domain.ViewModels
{
    // ViewModels (Razor Page)
    public class CrowdCalendarVm
    {
        public int? Id { get; set; }
        public DateTime DateUtc { get; set; } = DateTime.UtcNow.Date;
        public string RegionCode { get; set; } = "";
        public int? PlaceId { get; set; }
        public string? EventName { get; set; }
        public CrowdLevelEnum ExpectedLevel { get; set; } = CrowdLevelEnum.Medium;
        public byte? Confidence { get; set; }
        public TimeSpan? StartLocalTime { get; set; }
        public TimeSpan? EndLocalTime { get; set; }
        public int LeadHours { get; set; } = 3;
        public string? MessageTemplate { get; set; }
        public string? Tags { get; set; }
        public bool Active { get; set; } = true;
    }
}
