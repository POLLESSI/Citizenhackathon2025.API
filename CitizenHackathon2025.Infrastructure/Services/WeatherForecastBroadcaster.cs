using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WeatherForecastBroadcaster : IWeatherForecastBroadcaster
    {
        private readonly IHubContext<WeatherForecastHub> _hub;

        // WHY: Centralized event names => no string literals
        private static class HubEvents
        {
            public const string ReceiveForecast = WeatherForecastHubMethods.ToClient.ReceiveForecast;
            public const string HeavyRainAlert = WeatherForecastHubMethods.ToClient.HeavyRainAlert;
            public const string EventArchived = WeatherForecastHubMethods.ToClient.EventArchived;
        }

        public WeatherForecastBroadcaster(IHubContext<WeatherForecastHub> hub)
            => _hub = hub;

        public Task BroadcastForecastAsync(WeatherForecastDTO dto, CancellationToken ct = default)
            => _hub.Clients.All.SendAsync(HubEvents.ReceiveForecast, dto, ct);

        public Task BroadcastHeavyRainAlertAsync(RainAlertDTO alert, CancellationToken ct = default)
            => _hub.Clients.All.SendAsync(HubEvents.HeavyRainAlert, alert, ct);

        public Task BroadcastArchivedAsync(int id, CancellationToken ct = default)
            => _hub.Clients.All.SendAsync(HubEvents.EventArchived, id, ct);
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.