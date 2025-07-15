using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class WeatherForecastExtensions
    {
        public static WeatherForecastDTO MapToWeatherForecastDTO(this WeatherForecast forecast)
        {
            return new WeatherForecastDTO
            {
                Id = forecast.Id,
                DateWeather = forecast.DateWeather,
                Summary = forecast.Summary,
                TemperatureC = forecast.TemperatureC,
                RainfallMm = forecast.RainfallMm,
                Humidity = forecast.Humidity,
                WindSpeedKmh = forecast.WindSpeedKmh
            };
        }

        public static TrafficConditionDTO MapToTrafficConditionDTO(this WeatherForecast forecast)
        {
            return new TrafficConditionDTO
            {
                Location = "Default",
                Level = forecast.WindSpeedKmh > 60 ? "Severe" : "Moderate",
                Message = "Risk of gusts affecting traffic"
            };
        }

        public static CrowdInfoDTO MapToCrowdInfoDTO(this WeatherForecast forecast)
        {
            return new CrowdInfoDTO
            {
                Area = "City",
                CrowdLevel = forecast.Humidity > 80 ? "3" : "1", // Ex: high humidity → more people in indoor areas
                Icon = "people",
                Color = forecast.Humidity > 80 ? "#FF0000" : "#00FF00"
            };
        }

        public static SuggestionDTO MapToSuggestionDTO(this WeatherForecast forecast)
        {
            return new SuggestionDTO
            {
                Message = forecast.RainfallMm > 5
                    ? "Remember to take an umbrella"
                    : "Nice weather today",
                Context = "Local weather"
            };
        }
    }

}
