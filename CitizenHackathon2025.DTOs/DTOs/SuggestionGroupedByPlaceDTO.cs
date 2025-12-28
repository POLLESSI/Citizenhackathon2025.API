// Projet : CitizenHackathon2025.DTOs
using System;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class SuggestionGroupedByPlaceDTO
    {
        public string PlaceName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Indoor { get; set; }
        public string CrowdLevel { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int SuggestionCount { get; set; }
        public DateTime LastSuggestedAt { get; set; }
        // Here, you can put List<SuggestionDTO> if you want to remain 100% DTO
        public List<SuggestionDTO>? Suggestions { get; set; }
    }
}





























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.