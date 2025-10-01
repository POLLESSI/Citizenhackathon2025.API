// CitizenHackathon2025.Hubs/Extensions/NotificationHubContextExtensions.cs
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class NotificationHubContextExtensions
    {
        public static Task BroadcastNotify(this IHubContext<NotificationHub> ctx, string message) =>
            ctx.Clients.All.SendAsync(NotificationHubMethods.ToClient.Notify, message);

        public static Task NotifyUser(this IHubContext<NotificationHub> ctx, string userIdOrEmail, string message) =>
            ctx.Clients.All.SendAsync(NotificationHubMethods.ToClient.NotifyUser, userIdOrEmail, message);

        public static Task BroadcastSystem(this IHubContext<NotificationHub> ctx, string message) =>
            ctx.Clients.All.SendAsync(NotificationHubMethods.ToClient.System, message);
    }
}
