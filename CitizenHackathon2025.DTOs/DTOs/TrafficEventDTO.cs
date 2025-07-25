﻿using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class TrafficEventDTO
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public TrafficLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.