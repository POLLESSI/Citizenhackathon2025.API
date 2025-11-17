using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Contracts.Hubs;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class EventHubContextExtensions
    {
        /// <summary>
        /// Sends a NewEvent notification to all clients.
        /// </summary>
        public static Task SendNewEvent(this IHubContext<EventHub> hubContext, string message)
            => hubContext.Clients.All.SendAsync(EventHubMethods.ToClient.NewEvent, message);

        // Targeted variants
        public static Task SendNewEventToConnection(this IHubContext<EventHub> hubContext, string connectionId, string message)
            => hubContext.Clients.Client(connectionId).SendAsync(EventHubMethods.ToClient.NewEvent, message);

        public static Task SendNewEventToGroup(this IHubContext<EventHub> hubContext, string groupName, string message)
            => hubContext.Clients.Group(groupName).SendAsync(EventHubMethods.ToClient.NewEvent, message);

        public static Task BroadcastNewEvent(this IHubContext<EventHub> ctx, object payload)
           => ctx.Clients.All.SendAsync(EventHubMethods.ToClient.NewEvent, payload);
    }
}





















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.