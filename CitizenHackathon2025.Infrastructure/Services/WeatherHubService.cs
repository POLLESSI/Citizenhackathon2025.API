using CitizenHackathon2025.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Hubs.Hubs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherHubService
    {
        private readonly IHubContext<WeatherForecastHub> _hubContext;

        public WeatherHubService(IHubContext<WeatherForecastHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastWeatherAsync(WeatherForecast forecast, CancellationToken cancellationToken = default)
        {
            var dto = forecast.MapToWeatherForecastDTO();
            await _hubContext.Clients.All.SendAsync("ReceiveWeather", forecast, cancellationToken);
        }

        public Task SendWeatherToAllClientsAsync()
        {
            throw new NotImplementedException();
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.