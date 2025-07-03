using MediatR;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace Citizenhackathon2025.Application.CQRS.Commands
{
    public class CreateWeatherForecastCommand : IRequest<WeatherForecastDTO>
    {
    #nullable disable
        public DateTime DateWeather { get; set; }
        public int TemperatureC { get; set; }
        public string Summary { get; set; }
        public int Humidity { get; set; }
        public double RainfallMm { get; set; }
        public double WindSpeedKmh { get; set; }
    }
}















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.