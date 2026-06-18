namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiContextDTO
    {
        public string UserPrompt { get; init; } = string.Empty;
        public string? LocationLabel { get; init; }

        public DateTime TargetDate { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }

        public List<LocalAiPlaceContextDTO> Places { get; init; } = new();
        public List<LocalAiPlaceContextDTO> KeywordMatchedPlaces { get; set; } = new();
        public List<LocalAiEventContextDTO> Events { get; init; } = new();
        public List<LocalAiCrowdCalendarContextDTO> CrowdCalendar { get; init; } = new();
        public List<LocalAiCrowdInfoContextDTO> CrowdInfo { get; init; } = new();
        public List<LocalAiTrafficContextDTO> Traffic { get; init; } = new();
        public List<LocalAiWeatherContextDTO> Weather { get; init; } = new();
        public List<LocalAiCriticalAlertContextDTO> CriticalAlerts { get; set; } = new();

        public bool HasChildren { get; set; }
        public bool PreferIndoorWhenRain { get; set; } = true;
        public bool BadWeatherDetected { get; set; }
        public double MaxAlternativeRadiusKm { get; set; } = 25;
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.