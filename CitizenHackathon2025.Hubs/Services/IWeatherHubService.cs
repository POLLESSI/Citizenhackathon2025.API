using Citizenhackathon2025.Shared.DTOs;

namespace CitizenHackathon2025.Hubs.Services
{
    public interface IWeatherHubService
    {
        Task BroadcastWeatherAsync(WeatherForecastDTO forecastDto, CancellationToken cancellationToken = default);
        Task SendWeatherToAllClientsAsync();
    }
}
