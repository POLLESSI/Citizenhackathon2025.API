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















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.