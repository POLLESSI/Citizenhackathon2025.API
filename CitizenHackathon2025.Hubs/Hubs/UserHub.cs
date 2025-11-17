using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class UserHub : Hub
    {
        // Client -> Server
        public async Task NotifyUserRegistered(string email)
        {
            await Clients.All.SendAsync(UserHubMethods.ToClient.UserRegistered, email);
        }
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.