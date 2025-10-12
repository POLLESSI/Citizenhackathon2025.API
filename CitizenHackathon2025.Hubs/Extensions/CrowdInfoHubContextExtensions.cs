// CitizenHackathon2025.Hubs/Extensions/CrowdInfoHubContextExtensions.cs
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Extensions
{
    /// <summary>
    /// Message sending helpers for CrowdHub (avoids "magic strings").
    /// </summary>
    public static class CrowdInfoHubContextExtensions
    {
        // =========================
        // Internal helpers
        // =========================
        private static string AsJson(object payload) =>
            payload is string s ? s : JsonSerializer.Serialize(payload);

        // =========================
        // Broadcast (all clients)
        // =========================

        /// <summary>Broadcasts new "Crowd" info (payload string/JSON).</summary>
        public static Task BroadcastNewCrowdInfo(this IHubContext<CrowdHub> hub, string message, CancellationToken ct = default) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, message, ct);

        /// <summary>Broadcasts new "Crowd" info by serializing the payload to JSON.</summary>
        public static Task BroadcastNewCrowdInfo(this IHubContext<CrowdHub> hub, object payload, CancellationToken ct = default) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, AsJson(payload), ct);

        /// <summary>Broadcasts a generic "Crowd" update (payload string/JSON).</summary>
        public static Task BroadcastCrowdUpdate(this IHubContext<CrowdHub> hub, string message, CancellationToken ct = default) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, message, ct);

        /// <summary>Broadcasts a generic "Crowd" update by serializing the payload to JSON.</summary>
        public static Task BroadcastCrowdUpdate(this IHubContext<CrowdHub> hub, object payload, CancellationToken ct = default) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, AsJson(payload), ct);

        /// <summary>Reports the archiving of a "Crowd" info (without payload).</summary>
        public static Task BroadcastCrowdArchived(this IHubContext<CrowdHub> hub, int id, CancellationToken ct = default) =>
         hub.Clients.All.SendAsync(CrowdHubMethods.ToClient.CrowdInfoArchived, id, ct);

        /// <summary>Request a client-side refresh (ping with message).</summary>
        public static Task BroadcastCrowdRefreshRequested(this IHubContext<CrowdHub> hub, string message, CancellationToken ct = default) =>
            hub.Clients.All.SendAsync(CrowdHubMethods.ToClient.CrowdRefreshRequested, message, ct);

        // =========================
        // By group
        // =========================

        public static Task CrowdNewInfoToGroup(this IHubContext<CrowdHub> hub, string group, string message, CancellationToken ct = default) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, message, ct);

        public static Task CrowdNewInfoToGroup(this IHubContext<CrowdHub> hub, string group, object payload, CancellationToken ct = default) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, AsJson(payload), ct);

        public static Task CrowdUpdateToGroup(this IHubContext<CrowdHub> hub, string group, string message, CancellationToken ct = default) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, message, ct);

        public static Task CrowdUpdateToGroup(this IHubContext<CrowdHub> hub, string group, object payload, CancellationToken ct = default) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, AsJson(payload), ct);

        public static Task CrowdArchivedToGroup(this IHubContext<CrowdHub> hub, string group, CancellationToken ct = default) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ToClient.CrowdInfoArchived, ct);

        public static Task CrowdRefreshRequestedToGroup(this IHubContext<CrowdHub> hub, string group, string message, CancellationToken ct = default) =>
            hub.Clients.Group(group).SendAsync(CrowdHubMethods.ToClient.CrowdRefreshRequested, message, ct);

        // =========================
        // By connectionId
        // =========================

        public static Task CrowdNewInfoToConnection(this IHubContext<CrowdHub> hub, string connectionId, string message, CancellationToken ct = default) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, message, ct);

        public static Task CrowdNewInfoToConnection(this IHubContext<CrowdHub> hub, string connectionId, object payload, CancellationToken ct = default) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, AsJson(payload), ct);

        public static Task CrowdUpdateToConnection(this IHubContext<CrowdHub> hub, string connectionId, string message, CancellationToken ct = default) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, message, ct);

        public static Task CrowdUpdateToConnection(this IHubContext<CrowdHub> hub, string connectionId, object payload, CancellationToken ct = default) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, AsJson(payload), ct);

        public static Task CrowdArchivedToConnection(this IHubContext<CrowdHub> hub, string connectionId, CancellationToken ct = default) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ToClient.CrowdInfoArchived, ct);

        public static Task CrowdRefreshRequestedToConnection(this IHubContext<CrowdHub> hub, string connectionId, string message, CancellationToken ct = default) =>
            hub.Clients.Client(connectionId).SendAsync(CrowdHubMethods.ToClient.CrowdRefreshRequested, message, ct);

        // =========================
        // (Optional) By userId
        // =========================
        public static Task CrowdNewInfoToUser(this IHubContext<CrowdHub> hub, string userId, object payload, CancellationToken ct = default) =>
            hub.Clients.User(userId).SendAsync(CrowdHubMethods.ToClient.NewCrowdInfo, AsJson(payload), ct);

        public static Task CrowdUpdateToUser(this IHubContext<CrowdHub> hub, string userId, object payload, CancellationToken ct = default) =>
            hub.Clients.User(userId).SendAsync(CrowdHubMethods.ToClient.ReceiveCrowdUpdate, AsJson(payload), ct);

        public static Task CrowdArchivedToUser(this IHubContext<CrowdHub> hub, string userId, CancellationToken ct = default) =>
            hub.Clients.User(userId).SendAsync(CrowdHubMethods.ToClient.CrowdInfoArchived, ct);

        public static Task CrowdRefreshRequestedToUser(this IHubContext<CrowdHub> hub, string userId, string message, CancellationToken ct = default) =>
            hub.Clients.User(userId).SendAsync(CrowdHubMethods.ToClient.CrowdRefreshRequested, message, ct);
    }
}
