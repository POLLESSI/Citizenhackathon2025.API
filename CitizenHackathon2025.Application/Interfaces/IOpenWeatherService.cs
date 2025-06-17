using System.Threading.Tasks;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IOpenWeatherService
    {
        Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city);
        Task<string> GetWeatherSummaryAsync(string location);
        Task<WeatherForecastDTO?> GetForecastAsync(string city);
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.