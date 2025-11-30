using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    [Obsolete("Legacy MediatR query for WeatherForecast. Currently not used by OutZen API.")]
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

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.