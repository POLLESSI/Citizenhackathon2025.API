using MediatR;
using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.CQRS.Queries
{
    public record GetLatestWeatherForecastQuery() : IRequest<WeatherForecastDTO?>;
}



