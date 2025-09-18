using System.Threading.Tasks;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IOpenWeatherService
    {
        Task<(double lat, double lon)?> GetCoordinatesAsync(string city);

        // with ct
        Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city, CancellationToken ct = default);
        Task<WeatherForecastDTO?> GetForecastAsync(string city, CancellationToken ct = default);
        Task<WeatherForecastDTO> GetForecastAsync(decimal latitude, decimal longitude, CancellationToken ct = default);
        Task<WeatherForecastDTO?> GetWeatherAsync(double lat, double lon, CancellationToken ct = default);
        Task<string> GetWeatherSummaryAsync(string location, CancellationToken ct = default);

        // (optional) overloads without ct — if you keep them, the service implements them via wrappers:
        Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city);
        Task<WeatherForecastDTO?> GetForecastAsync(string city);
        Task<WeatherForecastDTO?> GetWeatherAsync(double lat, double lon);
        Task<string> GetWeatherSummaryAsync(string location);
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.