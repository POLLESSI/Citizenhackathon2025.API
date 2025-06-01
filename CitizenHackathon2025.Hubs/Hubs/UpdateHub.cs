using Microsoft.AspNetCore.SignalR;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class UpdateHub : Hub
    {
        public async Task SendUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveUpdate", message);
        }
    }
}
