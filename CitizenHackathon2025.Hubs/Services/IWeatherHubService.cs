using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Hubs.Services
{
    public interface IWeatherHubService
    {
        Task BroadcastWeatherAsync(WeatherForecastDTO forecastDto, CancellationToken cancellationToken = default);
        Task SendWeatherToAllClientsAsync();
    }
}