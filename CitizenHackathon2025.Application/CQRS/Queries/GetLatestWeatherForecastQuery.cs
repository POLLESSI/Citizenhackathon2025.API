using MediatR;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.CQRS.Queries
{
    public class GetLatestWeatherForecastQuery : IRequest<WeatherForecastDTO?>
    {
        public record GetLatestForecastQuery() : IRequest<WeatherForecastDTO?>;
    }
}

