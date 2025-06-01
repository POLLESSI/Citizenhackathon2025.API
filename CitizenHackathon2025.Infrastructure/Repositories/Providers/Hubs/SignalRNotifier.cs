using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Application.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Citizenhackathon2025.Infrastructure.Repositories.Providers.Hubs
{
    public class SignalRNotifier
    {
        private readonly IHubContext<WeatherHub> _hub;

        public async Task NotifyAsync(WeatherForecast forecast)
        {
            var dto = forecast.MapToWeatherForecastDTO(); // mapping
            await _hub.Clients.All.SendAsync("ReceiveForecast", dto);
        }
    }
}       
    
