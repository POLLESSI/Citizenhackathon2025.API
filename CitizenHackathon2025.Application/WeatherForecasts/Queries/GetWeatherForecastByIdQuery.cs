using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    public record GetWeatherForecastByIdQuery(int Id) : IRequest<WeatherForecastDTO?>;
}
