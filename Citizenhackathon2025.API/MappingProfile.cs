using AutoMapper;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;

namespace CitizenHackathon2025.API
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<WeatherForecast, WeatherForecastDTO>().ReverseMap();
        }
    }
}
