using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class SuggestionHubContextExtensions
    {
        public static Task BroadcastNewSuggestion(this IHubContext<SuggestionHub> ctx) =>
            ctx.Clients.All.SendAsync(SuggestionHubMethods.ToClient.NewSuggestion);

        public static Task BroadcastSuggestion(this IHubContext<SuggestionHub> ctx, SuggestionDTO dto) =>
            ctx.Clients.All.SendAsync(SuggestionHubMethods.ToClient.ReceiveSuggestion, dto);
    }
}
