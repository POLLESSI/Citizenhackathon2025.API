namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class ManualCrowdCriticalAlertRequest
    {
        public int PlaceId { get; set; }
        public string? Reason { get; set; }
        public string? Source { get; set; } = "ManualButton";
    }
}