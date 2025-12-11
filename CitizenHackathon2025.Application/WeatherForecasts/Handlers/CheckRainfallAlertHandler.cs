using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.WeatherForecasts.Commands;
using CitizenHackathon2025.Contracts.DTOs;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Application.WeatherForecasts.Handlers
{
    public class CheckRainfallAlertHandler :
        IRequestHandler<CheckRainfallAlertCommand, RainAlertDTO?>
    {
        private readonly IWeatherForecastService _service;

        public CheckRainfallAlertHandler(IWeatherForecastService service)
        {
            _service = service;
        }

        public Task<RainAlertDTO?> Handle( CheckRainfallAlertCommand request, CancellationToken ct)
        {
            return _service.CheckRainfallAlertAsync(request.Weather, ct);
        }
    }
}
