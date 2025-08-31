using CitizenHackathon2025.DTOs.DTOs;
using System.Globalization;

namespace CitizenHackathon2025.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for the user interface presentation logic.
    /// For example: colors, icons, visual formatting.
    /// </summary>
    public class UIService
    {
        public CrowdInfoUIDTO EnhanceCrowdInfo(CrowdInfoDTO dto)
        {
            int level = 0;
            if (!int.TryParse(dto.CrowdLevel, out level))
                level = 0;

            return new CrowdInfoUIDTO
            {
                Original = dto,
                Color = GetCrowdLevelColor(level),
                Icon = GetCrowdIcon(level),
                VisualLevel = GetCrowdLevelLabel(level)
            };
        }
        /// <summary>
        /// Gives a color based on crowd level.
        /// </summary>
        public string GetCrowdLevelColor(int crowdLevel)
        {
            return crowdLevel switch
            {
                <= 2 => "green",
                <= 5 => "orange",
                _ => "red"
            };
        }
        public string GetCrowdIcon(int level)
        {
            return level switch
            {
                <= 2 => "🟢",
                <= 5 => "🟠",
                <= 8 => "🔴",
                _ => "⚫"
            };
        }
        public string GetCrowdLevelLabel(int level)
        {
            return level switch
            {
                <= 2 => "Low attendance",
                <= 5 => "Moderate crowd",
                <= 8 => "Large crowd",
                _ => "Overcrowded area"
            };
        }

        /// <summary>
        /// Returns an icon representing the weather.
        /// </summary>
        public string GetWeatherIcon(string summary)
        {
            return summary.ToLowerInvariant() switch
            {
                "sunny" => "☀️",
                "rainy" => "🌧️",
                "stormy" => "⛈️",
                "snowy" => "❄️",
                "cloudy" => "☁️",
                _ => "🌤️"
            };
        }

        /// <summary>
        /// Adds UI metadata to a weather forecast.
        /// </summary>
        public WeatherForecastUIDTO EnhanceForecast(WeatherForecastDTO dto)
        {
            return new WeatherForecastUIDTO
            {
                Original = dto,
                Icon = GetWeatherIcon(dto.Summary),
                DisplayDate = dto.DateWeatherFormatted,
                TemperatureColor = GetTemperatureColor(dto.TemperatureC)
            };
        }

        public string GetTemperatureColor(int temperatureC)
        {
            return temperatureC switch
            {
                <= 0 => "blue",
                <= 15 => "teal",
                <= 30 => "orange",
                _ => "red"
            };
        }
        /// <summary>
        /// Returns a color based on congestion level.
        /// </summary>
        public string GetCongestionColor(string congestionLevel)
        {
            return congestionLevel?.ToUpperInvariant() switch
            {
                "L" => "green",     // Light
                "M" => "orange",    // Medium
                "H" => "red",       // High
                _ => "gray"
            };
        }

        /// <summary>
        /// Returns an emoji icon based on incident type.
        /// </summary>
        public string GetIncidentIcon(string incidentType)
        {
            if (string.IsNullOrWhiteSpace(incidentType)) return "🛣️";

            var type = incidentType.ToLowerInvariant();
            return type switch
            {
                var s when s.Contains("accident") => "💥",
                var s when s.Contains("construction") => "🚧",
                var s when s.Contains("flood") => "🌊",
                var s when s.Contains("roadblock") => "⛔",
                var s when s.Contains("fire") => "🔥",
                _ => "🚗"
            };
        }

        /// <summary>
        /// Enhances a TrafficConditionDTO with UI metadata.
        /// </summary>
        public TrafficConditionUIDTO EnhanceTraffic(TrafficConditionDTO dto)
        {
            return new TrafficConditionUIDTO
            {
                Original = dto,
                Icon = GetIncidentIcon(dto.IncidentType),
                DisplayDate = dto.DateCondition.ToString("dddd dd MMMM yyyy", CultureInfo.InvariantCulture),
                CongestionColor = GetCongestionColor(dto.CongestionLevel)
            };
        }
       
    }
}
public class WeatherForecastUIDTO
{
#nullable disable
    public WeatherForecastDTO Original { get; set; }
    public string Icon { get; set; }
    public string DisplayDate { get; set; }
    public string TemperatureColor { get; set; }
}

public class TrafficConditionUIDTO
{
#nullable disable
    public TrafficConditionDTO Original { get; set; }
    public string Icon { get; set; }
    public string DisplayDate { get; set; }
    public string CongestionColor { get; set; }
}








































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.