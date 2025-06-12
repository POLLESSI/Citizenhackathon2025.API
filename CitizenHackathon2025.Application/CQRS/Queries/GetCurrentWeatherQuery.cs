using Citizenhackathon2025.Shared.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Queries
{
    public record GetCurrentWeatherQuery() : IRequest<WeatherForecastDTO?>;
}
