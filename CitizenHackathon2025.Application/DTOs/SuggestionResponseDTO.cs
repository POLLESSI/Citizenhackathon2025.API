
using Citizenhackathon2025.Shared.DTOs;

namespace CitizenHackathon2025.Application.DTOs
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
