using Citizenhackathon2025.Shared.DTOs;
using MediatR;

namespace Citizenhackathon2025.Application.WeatherForecast.Queries
{
    public record GetLatestForecastQuery : IRequest<WeatherForecastDTO>;
}

