using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
using System.Collections.Generic;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    public record GetAllWeatherForecastsQuery() : IRequest<List<WeatherForecastDTO>>;
}
