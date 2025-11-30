namespace CitizenHackathon2025.Domain.Entities
{
    public class GPTInteraction
    {
#nullable disable
        public int Id { get; set; }
        public string Prompt { get; set; }
        public string Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; } = true;
        public string PromptHash { get; set; }

        // --- Evolution script; Optional links to other aggregates ---
        public int? EventId { get; set; }
        public int? CrowdInfoId { get; set; }
        public int? PlaceId { get; set; }
        public int? TrafficConditionId { get; set; }
        public int? WeatherForecastId { get; set; }

        // --- Coordinates “snapshot” at the moment of the interaction ---
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // --- Source for styling the front marker (optional) ---
        public string? SourceType { get; set; } // "Event", "Crowd", "Place", "Traffic", "Weather"
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.