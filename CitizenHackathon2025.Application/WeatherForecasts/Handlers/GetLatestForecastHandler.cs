using AutoMapper;
using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Domain.Interfaces;
//using CitizenHackathon2025.infrastructure.Repositories;
using Citizenhackathon2025.Shared.DTOs;
using MediatR;

namespace Citizenhackathon2025.Application.WeatherForecast.Handlers
{
    public class GetLatestForecastHandler : IRequestHandler<GetLatestForecastQuery, WeatherForecastDTO>
    {
        private readonly IWeatherForecastRepository _weatherForecastRepository;
        private readonly IMapper _mapper;

        public GetLatestForecastHandler(IWeatherForecastRepository weatherForecastRepository, IMapper mapper)
        {
            _weatherForecastRepository = weatherForecastRepository;
            _mapper = mapper;
        }

        public async Task<WeatherForecastDTO> Handle(GetLatestForecastQuery request, CancellationToken cancellationToken)
        {
            var model = await _weatherForecastRepository.GetLatestWeatherForecastAsync();
            return _mapper.Map<WeatherForecastDTO>(model);
        }
    }
}
