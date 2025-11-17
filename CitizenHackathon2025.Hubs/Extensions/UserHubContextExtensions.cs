using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Contracts.Hubs;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class UserHubContextExtensions
    {
        public static Task BroadcastUserRegistered(this IHubContext<UserHub> ctx, string email) =>
            ctx.Clients.All.SendAsync(UserHubMethods.ToClient.UserRegistered, email);
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.