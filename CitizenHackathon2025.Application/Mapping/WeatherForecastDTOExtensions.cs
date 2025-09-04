using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class WeatherForecastDTOExtensions
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
    }
}