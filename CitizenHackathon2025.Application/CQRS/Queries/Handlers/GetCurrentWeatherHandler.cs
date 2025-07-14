using CitizenHackathon2025.Application.Extensions;
using Citizenhackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Application.CQRS.Queries.Handlers
{
    public class GetCurrentWeatherHandler : IRequestHandler<GetCurrentWeatherQuery, WeatherForecastDTO?>
    {
        private readonly IWeatherForecastRepository _repository;
        private readonly ILogger<GetCurrentWeatherHandler> _logger;

        public GetCurrentWeatherHandler(IWeatherForecastRepository repository, ILogger<GetCurrentWeatherHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<WeatherForecastDTO?> Handle(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Weather query received at {Time} for city {City}", DateTime.UtcNow, request.City);

            var model = await _repository.GetLatestWeatherForecastAsync(); // <- Here you could add a filter by city if necessary
            if (model == null) return null;

            return model.MapToWeatherForecastDTO();
        }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.