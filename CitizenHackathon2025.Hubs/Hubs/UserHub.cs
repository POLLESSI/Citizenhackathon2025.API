using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
