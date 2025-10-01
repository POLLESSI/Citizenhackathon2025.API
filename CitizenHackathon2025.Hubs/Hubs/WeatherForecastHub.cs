using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class WeatherForecastHub : Hub
    {
    #nullable disable
        public async Task RefreshWeatherForecast(string message)
        {
            await Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.NewWeatherForecast, message);
        }

        public async Task Notify(string message)
        {
            await Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.ReceiveForecast, message);
        }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.