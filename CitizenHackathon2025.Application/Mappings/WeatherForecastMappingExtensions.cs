using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Application.Extensions;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class WeatherForecastMappingExtensions
    {
        public static WeatherForecastDTO ToDTO(this WeatherForecast entity)
            => CitizenHackathon2025.Application.Extensions.MapperExtensions.MapToWeatherForecastDTO(entity);

        public static WeatherForecast ToEntity(this WeatherForecastDTO dto)
            => dto.MapToWeatherForecast();

        public static WeatherInfoDTO ToWeatherInfoDTO(this WeatherForecastDTO dto, string city)
            => dto.MapToWeatherInfoDTO(city);
    }
}























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.