namespace CitizenHackathon2025.DTOs.DTOs
{
    public class GptInteractionDTO
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;

        public string PromptHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; }

        // ---- New: Context for the map / UI ----
        public int? EventId { get; set; }
        public int? CrowdInfoId { get; set; }
        public int? PlaceId { get; set; }
        public int? TrafficConditionId { get; set; }
        public int? WeatherForecastId { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? SourceType { get; set; }   // "Event", "Crowd", "Place", "Traffic", "Weather"
        public int? CrowdLevel { get; set; }      // useful in a crowd context
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.