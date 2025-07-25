﻿using CitizenHackathon2025.DTOs.Enums;

namespace CitizenHackathon2025.DTOs.DTOs
{
    #nullable disable
    public class CrowdLevelDTO
    {
        public int Id { get; set; }                   // Unique identifier
        public string PlaceName { get; set; } = "";    // Name of the place concerned
        public string Icon { get; set; } = "";         // Icon to display (name or image path)
        public string Color { get; set; } = "";        // CSS color code or name (eg: "red", "#FF0000")
        public CrowdLevelEnum CrowdLevel { get; set; } // Crowd level (e.g. "Low", "Moderate", "High")
        public DateTime Timestamp { get; set; }        // Date/time of the information
        public string Source { get; set; } = "";       // Data source (e.g. "Simulation", "User", etc.)
        public double Latitude { get; set; }           // Geographic coordinates
        public double Longitude { get; set; }
        public bool IsOverloaded { get; set; }
        public string Description { get; set; }
    }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.