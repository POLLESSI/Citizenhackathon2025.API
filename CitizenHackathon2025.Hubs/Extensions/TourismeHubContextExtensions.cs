// CitizenHackathon2025.Hubs/Extensions/TourismeHubContextExtensions.cs
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class TourismeHubContextExtensions
    {
        public static Task BroadcastSuggestions(this IHubContext<AISuggestionHub> ctx, string payload) =>
            ctx.Clients.All.SendAsync(TourismeHubMethods.ToClient.SuggestionsUpdated, payload);

        public static Task BroadcastTourismInfo(this IHubContext<AISuggestionHub> ctx, string message) =>
            ctx.Clients.All.SendAsync(TourismeHubMethods.ToClient.Info, message);

        public static Task BroadcastTourismError(this IHubContext<AISuggestionHub> ctx, string message) =>
            ctx.Clients.All.SendAsync(TourismeHubMethods.ToClient.Error, message);
    }
}
