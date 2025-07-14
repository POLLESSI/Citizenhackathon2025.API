using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IOpenWeatherService
    {
        Task<(double lat, double lon)?> GetCoordinatesAsync(string city);
        Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city);
        Task<WeatherForecastDTO?> GetForecastAsync(string city);
        Task<WeatherForecastDTO?> GetWeatherAsync(double lat, double lon);
        Task<string> GetWeatherSummaryAsync(string location);
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.