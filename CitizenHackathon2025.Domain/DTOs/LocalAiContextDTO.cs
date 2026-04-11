namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiContextDTO
    {
        public string UserPrompt { get; set; } = string.Empty;
        public string? LocationLabel { get; set; }

        public DateTime? TargetDate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public List<LocalAiEventContextDTO> Events { get; set; } = new();
        public List<LocalAiCrowdCalendarContextDTO> CrowdCalendar { get; set; } = new();
        public List<LocalAiCrowdInfoContextDTO> CrowdInfo { get; set; } = new();
        public List<LocalAiTrafficContextDTO> Traffic { get; set; } = new();
        public List<LocalAiWeatherContextDTO> Weather { get; set; } = new();
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.