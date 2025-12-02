using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.ValueObjects;
using System;

namespace CitizenHackathon2025.Domain.Entities
{
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateTime DateWeather { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string? Summary { get; set; }
        public double RainfallMm { get; set; }
        public int Humidity { get; set; }
        public double WindSpeedKmh { get; set; }
        public WeatherType WeatherType { get; set; }
        public SeverityLevel Severity { get; set; }
        public string? Icon { get; set; } = string.Empty; // ex: 10d
        public string? IconUrl { get; set; } = string.Empty; // ex: https://openweathermap.org/img/wn/{icon}.png
        public string? WeatherMain { get; set; } = string.Empty;
        public bool IsSevere { get; set; } = false;
        public string? Description { get; set; }
        public bool Active { get; private set; } = true;
    }
    // DEPRECATED: Migrer to WeatherForecast DDD
    //[Obsolete("Use the DDD-based WeatherForecast entity instead.")]
    //public class LegacyWeatherForecast { ... }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.