using CitizenHackathon2025.Application.WeatherForecasts.Commands;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.ValueObjects;
using CitizenHackathon2025.DTOs.DTOs;
using Mapster;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Application.WeatherForecasts.Handlers
{
    /// <summary>
    /// Creates a weather forecast from the data provided by the command
    /// calculates the severity, persists via the repository, and returns a DTO.
    /// </summary>
    public sealed class CreateWeatherForecastHandler
        : IRequestHandler<CreateWeatherForecastCommand, WeatherForecastDTO>
    {
        private readonly IWeatherForecastRepository _repository;

        public CreateWeatherForecastHandler(IWeatherForecastRepository repository)
        {
            _repository = repository;
        }

        public async Task<WeatherForecastDTO> Handle(
            CreateWeatherForecastCommand request,
            CancellationToken cancellationToken)
        {
            // 1) Calculation of business severity from measurements
            var severity = WeatherSeverity.FromMetrics(
                request.WeatherType,
                request.TemperatureC,
                request.WindSpeedKmh,
                request.PrecipitationMm
            );

            // 2) Construction of the domain model (or record) expected by the repo
            //    Note: It is assumed that the model returned by InsertAsync is
            //    Mapster compatible -> WeatherForecastDTO (as for GetLatest).
            var temperatureInt = (int)Math.Round(request.TemperatureC, MidpointRounding.AwayFromZero);

            var entity = new WeatherForecast
            {
                Summary = request.LocationName,
                DateWeather = request.Date,
                TemperatureC = temperatureInt,
                WindSpeedKmh = request.WindSpeedKmh,
                RainfallMm = request.PrecipitationMm,
                WeatherType = request.WeatherType,
                Severity = severity.Level // même type que dans l'entité
            };

            // 3) Persistence (InsertAsync returns the model created on the domain side)
            var created = await _repository.InsertAsync(entity, cancellationToken);
            return created.Adapt<WeatherForecastDTO>();

            // 4) Mapping to DTO for the response
            return created.Adapt<WeatherForecastDTO>();
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.