using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Citizenhackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application.Extensions;

namespace Citizenhackathon2025.Application.Services
{
    public class WeatherHubService
    {
        private readonly IHubContext<Hubs.Hubs.WeatherHub> _hubContext;

        public WeatherHubService(IHubContext<Hubs.Hubs.WeatherHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastWeatherAsync(Domain.Entities.WeatherForecast forecast, CancellationToken cancellationToken = default)
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