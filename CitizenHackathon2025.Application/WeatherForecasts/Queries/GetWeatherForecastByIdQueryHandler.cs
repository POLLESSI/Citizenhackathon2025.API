using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    public class GetWeatherForecastByIdQueryHandler : IRequestHandler<GetWeatherForecastByIdQuery, WeatherForecastDTO?>
    {
        private readonly IWeatherForecastService _service;

        public GetWeatherForecastByIdQueryHandler(IWeatherForecastService service)
        {
            _service = service;
        }

        public async Task<WeatherForecastDTO?> Handle(GetWeatherForecastByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByIdAsync(request.Id);
        }
    }
}
