using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Application.Extensions;
using Citizenhackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Common.MediaR
{
#nullable disable
    public class GetWeatherQueryHandler : HandlerBase<GetWeatherQuery, WeatherForecastDTO>
    {

        private readonly IWeatherForecastRepository _repo;
        public GetWeatherQueryHandler(IWeatherForecastRepository repo, ILogger<GetWeatherQueryHandler> logger)
        : base(logger)
        {
            _repo = repo;
        }
        protected override async Task<WeatherForecastDTO> HandleRequest(GetWeatherQuery request, CancellationToken cancellationToken)
        {
            var model = await _repo.GetLatestWeatherForecastAsync();
            return model?.MapToWeatherForecastDTO() ?? new WeatherForecastDTO
            {
                DateWeather = DateTime.Now,
                TemperatureC = 0,
                Summary = "No data available",
                RainfallMm = 0.0,
                Humidity = 0,
                WindSpeedKmh = 0.0
            };
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.