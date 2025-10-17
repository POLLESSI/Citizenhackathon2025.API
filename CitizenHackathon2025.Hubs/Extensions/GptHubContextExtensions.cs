using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class GptHubContextExtensions
    {
        /// <summary>Broadcast NotifyNewGpt to all clients.</summary>
        public static Task SendNotifyNewGpt(this IHubContext<GPTHub> hubContext, string message) =>
            hubContext.Clients.All.SendAsync(GptInteractionHubMethods.ToClient.NotifyNewGpt, message);

        public static Task SendNotifyNewGptToConnection(this IHubContext<GPTHub> hubContext, string connectionId, string message) =>
            hubContext.Clients.Client(connectionId).SendAsync(GptInteractionHubMethods.ToClient.NotifyNewGpt, message);

        public static Task SendNotifyNewGptToGroup(this IHubContext<GPTHub> hubContext, string groupName, string message) =>
            hubContext.Clients.Group(groupName).SendAsync(GptInteractionHubMethods.ToClient.NotifyNewGpt, message);
    }
}


























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.