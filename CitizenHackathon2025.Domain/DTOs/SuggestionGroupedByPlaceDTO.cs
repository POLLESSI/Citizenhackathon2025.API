using CitizenHackathon2025.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Domain.DTOs
{
    public class SuggestionGroupedByPlaceDTO
    {
    #nullable disable
        public string PlaceName { get; set; }
        public string Type { get; set; }
        public bool Indoor { get; set; }
        public string CrowdLevel { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; } = string.Empty;
        public int SuggestionCount { get; set; }
        public DateTime LastSuggestedAt { get; set; }
        public List<Suggestion> Suggestions { get; set; } = new();
    }
}
