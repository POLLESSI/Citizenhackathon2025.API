﻿namespace CitizenHackathon2025.DTOs.DTOs
{
    public class WeatherInfoDTO
    {
        public string Location { get; set; } = string.Empty;
        public double TemperatureCelsius { get; set; }
        public double FeelsLikeCelsius { get; set; }
        public string WeatherDescription { get; set; } = string.Empty;
        public double WindSpeedKmh { get; set; }
        public double HumidityPercent { get; set; }
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
        public DateTime? Sunrise { get; set; }
        public DateTime? Sunset { get; set; }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.