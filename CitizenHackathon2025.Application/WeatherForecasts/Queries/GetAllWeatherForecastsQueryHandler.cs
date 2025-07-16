using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    public class GetAllWeatherForecastsQueryHandler : IRequestHandler<GetAllWeatherForecastsQuery, List<WeatherForecastDTO>>
    {
        private readonly IWeatherForecastService _service;

        public GetAllWeatherForecastsQueryHandler(IWeatherForecastService service)
        {
            _service = service;
        }

        public async Task<List<WeatherForecastDTO>> Handle(GetAllWeatherForecastsQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetAllAsync();
        }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.