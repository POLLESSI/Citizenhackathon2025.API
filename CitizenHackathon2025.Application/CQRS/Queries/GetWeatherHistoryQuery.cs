using MediatR;
using System.Collections.Generic;
using Citizenhackathon2025.Shared.DTOs;

namespace CitizenHackathon2025.Application.CQRS.Queries
{
    public record GetWeatherHistoryQuery(int Limit) : IRequest<List<WeatherForecastDTO>>;
}

