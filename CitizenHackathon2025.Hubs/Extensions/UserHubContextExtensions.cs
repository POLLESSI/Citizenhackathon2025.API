using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class UserHubContextExtensions
    {
        public static Task BroadcastUserRegistered(this IHubContext<UserHub> ctx, string email) =>
            ctx.Clients.All.SendAsync(UserHubMethods.ToClient.UserRegistered, email);
    }
}
