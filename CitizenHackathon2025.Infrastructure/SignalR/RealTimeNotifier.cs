using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Infrastructure.SignalR
{
    public class RealTimeNotifier : IRealTimeNotifier
    {
        private readonly IHubContext<OutZenHub> _hub;

        public RealTimeNotifier(IHubContext<OutZenHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyCrowdInfoUpdate(CrowdInfoUIDTO dto) =>
            _hub.Clients.All.SendAsync("CrowdInfoUpdated", dto);

        public Task NotifyWeatherForecast(WeatherForecastDTO dto) =>
            _hub.Clients.All.SendAsync("WeatherUpdated", dto);

        public Task NotifyTrafficCondition(TrafficConditionDTO dto) =>
            _hub.Clients.All.SendAsync("TrafficUpdated", dto);
    }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.