using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;


namespace CitizeHackathon2025.Hubs.Hubs
{
    public class WeatherForecastHub : Hub
    {
#nullable disable
        public async Task RefreshWeatherForecast(string message)
        {
            
            await Clients.All.SendAsync("NewWeatherForecast", message);
        }
    }
}
