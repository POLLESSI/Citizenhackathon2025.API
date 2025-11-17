using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Contracts.Hubs;

namespace CitizenHackathon2025.Hubs.Extensions
{
    /// <summary>
    /// TrafficHub message sending helpers (avoids literal strings).
    /// </summary>
    public static class TrafficConditionHubContextExtensions
    {
        /// <summary>
        /// Broadcast of the traffic refresh ping (without payload).
        /// Equivalent to: Clients.All.SendAsync("notifynewtraffic").
        /// </summary>
        public static Task BroadcastNotifyNewTraffic(this IHubContext<TrafficHub> hub)
            => hub.Clients.All.SendAsync(TrafficConditionHubMethods.ToClient.NotifyNewTraffic);

        /// <summary>
        /// Sending refresh ping to a group.
        /// </summary>
        public static Task NotifyNewTrafficToGroup(this IHubContext<TrafficHub> hub, string groupName)
            => hub.Clients.Group(groupName).SendAsync(TrafficConditionHubMethods.ToClient.NotifyNewTraffic);

        /// <summary>
        /// Sending refresh ping to a specific connection.
        /// </summary>
        public static Task NotifyNewTrafficToConnection(this IHubContext<TrafficHub> hub, string connectionId)
            => hub.Clients.Client(connectionId).SendAsync(TrafficConditionHubMethods.ToClient.NotifyNewTraffic);

        // --- Future extension points (if you add payloads/DTOs) ---
        // public static Task BroadcastTrafficUpdated(this IHubContext<TrafficHub> hub, TrafficDTO dto)
        //     => hub.Clients.All.SendAsync(TrafficConditionHubMethods.ToClient.TrafficUpdated, dto);
        //
        // public static Task TrafficCleared(this IHubContext<TrafficHub> hub)
        //     => hub.Clients.All.SendAsync(TrafficConditionHubMethods.ToClient.TrafficCleared);
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.