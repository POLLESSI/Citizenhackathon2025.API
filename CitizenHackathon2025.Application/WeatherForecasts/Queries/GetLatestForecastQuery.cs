using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    public record GetLatestForecastQuery : IRequest<WeatherForecastDTO>
    {
        public GetLatestForecastQuery(int id)
        {
            Id = id;
        }
        public int Id { get; init; } = 0; // Default to 0 for latest forecast
        public string? DateWeather { get; init; } = null; // Optional date filter, if provided will return the forecast for that date
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.