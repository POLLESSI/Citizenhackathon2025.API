using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;
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
