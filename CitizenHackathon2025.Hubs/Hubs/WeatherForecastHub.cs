using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class WeatherForecastHub : Hub
    {
    #nullable disable
        public async Task RefreshWeatherForecast(string message)
        {
            
            await Clients.All.SendAsync("NewWeatherForecast", message);
        }
        public async Task Notify(string message)
        {
            await Clients.All.SendAsync("ReceiveForecast", message);
        }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.