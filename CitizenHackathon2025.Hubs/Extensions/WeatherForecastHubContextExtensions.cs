using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class WeatherForecastHubContextExtensions
    {
        public static Task BroadcastNewForecast(this IHubContext<WeatherForecastHub> ctx, string payload) =>
            ctx.Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.NewWeatherForecast, payload);

        public static Task BroadcastForecastMessage(this IHubContext<WeatherForecastHub> ctx, string message) =>
            ctx.Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.ReceiveForecast, message);
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.