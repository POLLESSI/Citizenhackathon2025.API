using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;

namespace CitizenHackathon2025.Hubs.Services
{
    public class WeatherHubService : IWeatherHubService
    {
        private readonly IHubContext<WeatherForecastHub> _hubContext;

        public WeatherHubService(IHubContext<WeatherForecastHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task BroadcastWeatherAsync(WeatherForecastDTO forecastDto, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveForecast", forecastDto, cancellationToken);
        }
        public Task SendWeatherToAllClientsAsync()
        {
            throw new NotImplementedException();
        }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.