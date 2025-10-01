// CitizenHackathon2025.Hubs/Extensions/CrowdInfoHubContextExtensions.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Hubs.Extensions
{
    /// <summary>
    /// Message sending helpers for CrowdHub (avoids string literals).
    /// </summary>
    public static class CrowdInfoHubContextExtensions
    {
        // ---- Broadcast (all clients) ----

        /// <summary>Broadcasts a crowd update to all clients.</summary>
        public static Task BroadcastCrowdUpdate(this IHubContext<CrowdHub> hub, CrowdInfo crowd) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.ReceiveCrowdUpdate, crowd);

        /// <summary>Reports the archiving of a CrowdInfo to all clients.</summary>
        public static Task BroadcastCrowdArchived(this IHubContext<CrowdHub> hub, int crowdId) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.CrowdInfoArchived, crowdId);

        /// <summary>Request a refresh on the client side (ping without DTO).</summary>
        public static Task BroadcastCrowdRefreshRequested(this IHubContext<CrowdHub> hub, string message) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.CrowdRefreshRequested, message);

        // ---- Group targeting ----

        public static Task CrowdUpdateToGroup(this IHubContext<CrowdHub> hub, string group, CrowdInfo crowd) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ReceiveCrowdUpdate, crowd);

        public static Task CrowdArchivedToGroup(this IHubContext<CrowdHub> hub, string group, int crowdId) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.CrowdInfoArchived, crowdId);

        public static Task CrowdRefreshRequestedToGroup(this IHubContext<CrowdHub> hub, string group, string message) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.CrowdRefreshRequested, message);

        // ---- Targeting by connectionId ----

        public static Task CrowdUpdateToConnection(this IHubContext<CrowdHub> hub, string connectionId, CrowdInfo crowd) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ReceiveCrowdUpdate, crowd);

        public static Task CrowdArchivedToConnection(this IHubContext<CrowdHub> hub, string connectionId, int crowdId) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.CrowdInfoArchived, crowdId);

        public static Task CrowdRefreshRequestedToConnection(this IHubContext<CrowdHub> hub, string connectionId, string message) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.CrowdRefreshRequested, message);
    }
}
