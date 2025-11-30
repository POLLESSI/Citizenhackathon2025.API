using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Mapster;

namespace CitizenHackathon2025.Application.Mapping
{
    [Obsolete("Use OpenWeatherOptions from configuration instead of static constants.")]
    public class WeatherForecastMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<WeatherForecast, WeatherForecastDTO>()
                .Map(dest => dest.Summary, src => src.Summary)
                .Map(dest => dest.TemperatureC, src => src.TemperatureC)
                .Map(dest => dest.Humidity, src => src.Humidity)
                .Map(dest => dest.WindSpeedKmh, src => src.WindSpeedKmh)
                .Map(dest => dest.RainfallMm, src => src.RainfallMm);

        }
    }
}















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.