using Citizenhackathon2025.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Citizenhackathon2025.Infrastructure.Repositories.Providers.Hubs
{
    public class WeatherHub : Hub
    {
        public async Task Broadcast(WeatherForecastDTO data) =>
            await Clients.All.SendAsync("ReceiveForecast", data);
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.