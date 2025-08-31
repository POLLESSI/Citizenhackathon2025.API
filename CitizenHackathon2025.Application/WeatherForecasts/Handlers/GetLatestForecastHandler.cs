using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Mapster;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Handlers
{
    public class GetLatestForecastHandler : IRequestHandler<GetLatestForecastQuery, WeatherForecastDTO>
    {
        private readonly IWeatherForecastRepository _weatherForecastRepository;

        public GetLatestForecastHandler(IWeatherForecastRepository weatherForecastRepository)
        {
            _weatherForecastRepository = weatherForecastRepository;
        }

        public async Task<WeatherForecastDTO> Handle(GetLatestForecastQuery request, CancellationToken cancellationToken)
        {
            var model = await _weatherForecastRepository.GetLatestWeatherForecastAsync();
            return model.Adapt<WeatherForecastDTO>();
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.