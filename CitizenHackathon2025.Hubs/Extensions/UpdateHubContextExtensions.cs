// CitizenHackathon2025.Hubs/Extensions/UpdateHubContextExtensions.cs
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class UpdateHubContextExtensions
    {
        public static Task BroadcastUpdateAvailable(this IHubContext<UpdateHub> ctx, string version)
            => ctx.Clients.All.SendAsync(UpdateHubMethods.ToClient.UpdateAvailable, version);

        public static Task BroadcastConfigChanged(this IHubContext<UpdateHub> ctx, string keyOrJson)
            => ctx.Clients.All.SendAsync(UpdateHubMethods.ToClient.ConfigChanged, keyOrJson);

        public static Task BroadcastUpdateProgress(this IHubContext<UpdateHub> ctx, int percent)
            => ctx.Clients.All.SendAsync(UpdateHubMethods.ToClient.UpdateProgress, percent);
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.