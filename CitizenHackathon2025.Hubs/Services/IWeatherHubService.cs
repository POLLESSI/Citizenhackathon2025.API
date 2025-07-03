using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Hubs.Services
{
    public interface IWeatherHubService
    {
        Task BroadcastWeatherAsync(WeatherForecastDTO forecastDto, CancellationToken cancellationToken = default);
        Task SendWeatherToAllClientsAsync();
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.