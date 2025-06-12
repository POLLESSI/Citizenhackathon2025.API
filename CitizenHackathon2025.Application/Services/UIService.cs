using System;
using System.Collections.Generic;
using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.Services
{
    /// <summary>
    /// Service responsible for the user interface presentation logic.
    /// For example: colors, icons, visual formatting.
    /// </summary>
    public class UIService
    {
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

        public string GetTemperatureColor(string temperatureC)
        {
            if (!int.TryParse(temperatureC, out int temp))
                return "gray";

            return temp switch
            {
                <= 0 => "blue",
                <= 15 => "teal",
                <= 30 => "orange",
                _ => "red"
            };
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
}
