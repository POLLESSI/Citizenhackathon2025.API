using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class WeatherForecastHubContextExtensions
    {
        public static Task BroadcastNewForecast( this IHubContext<WeatherForecastHub> ctx, WeatherForecastDTO dto, CancellationToken ct = default)
            => ctx.Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.ReceiveForecast, dto, ct);

        public static Task BroadcastForecastArchived(this IHubContext<WeatherForecastHub> ctx, int id, CancellationToken ct = default)
            => ctx.Clients.All.SendAsync(WeatherForecastHubMethods.ToClient.EventArchived, id, ct);
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.