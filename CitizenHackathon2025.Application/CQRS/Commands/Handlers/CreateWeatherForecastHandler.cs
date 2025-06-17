using MediatR;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Application.Extensions;

namespace Citizenhackathon2025.Application.CQRS.Commands.Handlers
{
    public class CreateWeatherForecastHandler : IRequestHandler<CreateWeatherForecastCommand, WeatherForecastDTO>
    {
        private readonly IWeatherForecastRepository _repository;

        public CreateWeatherForecastHandler(IWeatherForecastRepository repository)
        {
            _repository = repository;
        }

        public async Task<WeatherForecastDTO> Handle(CreateWeatherForecastCommand request, CancellationToken cancellationToken)
        {
            var forecast = new WeatherForecastDTO
            {
                DateWeather = request.DateWeather,
                TemperatureC = request.TemperatureC,
                Summary = request.Summary,
                Humidity = request.Humidity,
                RainfallMm = request.RainfallMm,
                WindSpeedKmh = request.WindSpeedKmh
            };

            var entity = forecast.MapToWeatherForecast();
            var savedEntity = await _repository.SaveWeatherForecastAsync(entity);

            return savedEntity.MapToWeatherForecastDTO();
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.