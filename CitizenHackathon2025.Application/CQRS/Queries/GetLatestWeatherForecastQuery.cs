using MediatR;
using CitizenHackathon2025.DTOs.DTOs;

namespace Citizenhackathon2025.Application.CQRS.Queries
{
    public record GetLatestWeatherForecastQuery() : IRequest<WeatherForecastDTO?>;
}





































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.