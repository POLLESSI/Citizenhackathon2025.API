using Microsoft.AspNetCore.SignalR;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class UserHub : Hub
    {
        public async Task NotifyUserRegistered(string email)
        {
            await Clients.All.SendAsync("UserRegistered", email);
        }
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.