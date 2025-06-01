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
                TemperatureC = request.TemperatureC.ToString(),
                Summary = request.Summary,
                Humidity = request.Humidity.ToString(),
                RainfallMm = request.RainfallMm.ToString(),
                WindSpeedKmh = request.WindSpeedKmh.ToString()
            };

            var entity = forecast.MapToWeatherForecast();
            var savedEntity = await _repository.SaveWeatherForecastAsync(entity);

            return savedEntity.MapToWeatherForecastDTO();
        }
    }
}
