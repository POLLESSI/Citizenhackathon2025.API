namespace CitizenHackathon2025.DTOs.DTOs
{
    public class SuggestionResponseDTO
    {
        public string Location { get; set; } = string.Empty;
        public WeatherInfoDTO? Weather { get; set; }
        public CrowdInfoDTO? CrowdInfo { get; set; }
        public TrafficConditionDTO? TrafficInfo { get; set; }
        public string? AiSuggestion { get; set; }
        public string? Error { get; set; }
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.