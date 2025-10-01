using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class PlaceHubContextExtensions
    {
        /// <summary>Broadcast NewPlace to all customers.</summary>
        public static Task SendNewPlace(this IHubContext<PlaceHub> hubContext, string message) =>
            hubContext.Clients.All.SendAsync(PlaceHubMethods.ToClient.NewPlace, message);

        public static Task SendNewPlaceToConnection(this IHubContext<PlaceHub> hubContext, string connectionId, string message) =>
            hubContext.Clients.Client(connectionId).SendAsync(PlaceHubMethods.ToClient.NewPlace, message);

        public static Task SendNewPlaceToGroup(this IHubContext<PlaceHub> hubContext, string groupName, string message) =>
            hubContext.Clients.Group(groupName).SendAsync(PlaceHubMethods.ToClient.NewPlace, message);
    }
}
