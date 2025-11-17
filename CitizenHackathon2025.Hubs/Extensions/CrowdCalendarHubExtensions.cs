using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class CrowdCalendarHubExtensions
    {
        // ---- Mapping in Program.cs ----
        // Example:
        // var hubs = app.MapGroup("/hubs").RequireAuthorization();
        // hubs.MapCrowdCalendarHub(o => { o.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents; });
        public static RouteGroupBuilder MapCrowdCalendarHub(this RouteGroupBuilder group, Action<HttpConnectionDispatcherOptions>? configure = null)
        {
            group.MapHub<CitizenHackathon2025.Hubs.Hubs.CrowdCalendarHub>(CrowdCalendarHubMethods.HubPath, configure);
            return group;
        }

        // Variant if you want to map out of group:
        public static IEndpointConventionBuilder MapCrowdCalendarHub(this IEndpointRouteBuilder endpoints, Action<HttpConnectionDispatcherOptions>? configure = null)
            => endpoints.MapHub<CitizenHackathon2025.Hubs.Hubs.CrowdCalendarHub>(CrowdCalendarHubMethods.HubPath, configure);

        // ---- Send Helpers (from any service) ----
        public static Task BroadcastAdvisoryToRegion(this IHubContext<CitizenHackathon2025.Hubs.Hubs.CrowdCalendarHub> ctx, string regionCode, object payload)
            => ctx.Clients
                  .Group(CrowdCalendarHubMethods.RegionGroup(regionCode))
                  .SendAsync(CrowdCalendarHubMethods.ReceiveAdvisory, payload);

        public static Task BroadcastAdvisoriesToRegion(this IHubContext<CitizenHackathon2025.Hubs.Hubs.CrowdCalendarHub> ctx, string regionCode, IEnumerable<object> payload)
            => ctx.Clients
                  .Group(CrowdCalendarHubMethods.RegionGroup(regionCode))
                  .SendAsync(CrowdCalendarHubMethods.ReceiveAdvisories, payload);

        public static Task BroadcastAdvisoryToPlace(this IHubContext<CitizenHackathon2025.Hubs.Hubs.CrowdCalendarHub> ctx, int placeId, object payload)
            => ctx.Clients
                  .Group(CrowdCalendarHubMethods.PlaceGroup(placeId))
                  .SendAsync(CrowdCalendarHubMethods.ReceiveAdvisory, payload);

        public static Task BroadcastCalendarUpdated(this IHubContext<CitizenHackathon2025.Hubs.Hubs.CrowdCalendarHub> ctx)
            => ctx.Clients.All.SendAsync(CrowdCalendarHubMethods.ReceiveCalendarUpdated, new { updated = true });
    }
}


























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.