using System;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Commands
{
    /// <summary>
    /// Command to create a weather forecast.
    /// The fields correspond to the measurements needed to calculate severity.
    /// </summary>
    public sealed class CreateWeatherForecastCommand : IRequest<WeatherForecastDTO>
    {
        public string LocationName { get; init; } = default!;
        public DateTime Date { get; init; }

        public double TemperatureC { get; init; }
        public double WindSpeedKmh { get; init; }
        public double PrecipitationMm { get; init; }

        public WeatherType WeatherType { get; init; } = WeatherType.Unknown;
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.