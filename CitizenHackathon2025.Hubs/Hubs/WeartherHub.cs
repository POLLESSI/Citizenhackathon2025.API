using Citizenhackathon2025.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class WeatherHub : Hub
    {
        public async Task Notify(string message)
        {
            await Clients.All.SendAsync("ReceiveForecast", message);
        } 
    }
}
