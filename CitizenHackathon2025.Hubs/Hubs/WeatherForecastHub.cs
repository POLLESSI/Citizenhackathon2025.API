using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class WeatherForecastHub : Hub
    {
        public Task RefreshWeatherForecast(string message)
            => Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.ReceiveForecast, message);

        public Task Notify(string message)
            => Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.EventArchived, message);
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.