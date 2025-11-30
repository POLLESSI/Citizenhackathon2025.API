using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Mappings
{
    [Obsolete("Use OpenWeatherOptions from configuration instead of static constants.")]
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
                //Id = 0, 
                LocationName = "City", 
                Latitude = 0,          
                Longitude = 0,
                CrowdLevel = forecast.Humidity > 80 ? 3 : 1, 
                Timestamp = forecast.DateWeather
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










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.