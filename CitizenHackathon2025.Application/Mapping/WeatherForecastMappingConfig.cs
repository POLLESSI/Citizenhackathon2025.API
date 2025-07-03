using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Mapster;

namespace CitizenHackathon2025.Application.Mapping
{
    public class WeatherForecastMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<WeatherForecast, WeatherForecastDTO>()
                .Map(dest => dest.Summary, src => src.Summary);
            // etc...
        }
    }
}
