using MediatR;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.WeatherForecasts.Queries
{
    public class GetWeatherQuery : IRequest<WeatherForecastDTO?>
    {
        // You can add parameters here if needed, for example a date filter or location
        // public DateTime? Date { get; set; }
        public string? DateWeather { get; init; } = null;
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.