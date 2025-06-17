using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class SignalRNotifier : IHubNotifier
    {
        private readonly IHubContext<WeatherHub> _hubContext;

        public SignalRNotifier(IHubContext<WeatherHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task NotifyAsync(string message)
        {
            
            await _hubContext.Clients.All.SendAsync("ReceiveForecast", message);
        }

    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.