using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class SignalRNotifier : IHubNotifier
    {
        private readonly IHubContext<WeatherForecastHub> _hubContext;

        public SignalRNotifier(IHubContext<WeatherForecastHub> hubContext)
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