﻿using Citizenhackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Hubs.Services
{
    public class WeatherHubService : IWeatherHubService
    {
        private readonly IHubContext<WeatherHub> _hubContext;

        public WeatherHubService(IHubContext<WeatherHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task BroadcastWeatherAsync(WeatherForecastDTO forecastDto, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveForecast", forecastDto, cancellationToken);
        }
        public Task SendWeatherToAllClientsAsync()
        {
            throw new NotImplementedException();
        }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.