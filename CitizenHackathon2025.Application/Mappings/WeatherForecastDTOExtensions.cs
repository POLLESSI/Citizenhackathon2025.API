using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Shared.Extensions
{
    public static class WeatherForecastDTOExtensions
    {
        public static WeatherForecast? ToNumeric(this WeatherForecastDTO dto)
        {
            if (dto == null) return null;

            try
            {
                return new WeatherForecast
                {
                    Id = dto.Id,
                    DateWeather = dto.DateWeather,
                    TemperatureC = dto.TemperatureC, 
                    RainfallMm = dto.RainfallMm,     
                    Humidity = dto.Humidity,         
                    WindSpeedKmh = dto.WindSpeedKmh, 
                    Summary = dto.Summary ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToNumeric : {ex.Message}");
                return null;
            }
        }
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.