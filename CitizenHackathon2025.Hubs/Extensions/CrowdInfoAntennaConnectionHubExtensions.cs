using System;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class CrowdInfoAntennaConnectionHubExtensions
    {
        public static RouteGroupBuilder MapCrowdInfoAntennaConnectionHub(this RouteGroupBuilder group, Action<HttpConnectionDispatcherOptions>? configure = null)
        {
            group.MapHub<CrowdInfoAntennaConnectionHub>(CrowdInfoAntennaConnectionHubMethods.HubPath, configure);
            return group;
        }

        public static IEndpointConventionBuilder MapCrowdInfoAntennaConnectionHub(this IEndpointRouteBuilder endpoints, Action<HttpConnectionDispatcherOptions>? configure = null)
            => endpoints.MapHub<CrowdInfoAntennaConnectionHub>(CrowdInfoAntennaConnectionHubMethods.HubPath, configure);
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.