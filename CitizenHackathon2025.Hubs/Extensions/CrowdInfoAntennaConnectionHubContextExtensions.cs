using System.Threading;
using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class CrowdInfoAntennaConnectionHubContextExtensions
    {
        public static Task BroadcastAntennaCounts(this IHubContext<CrowdInfoAntennaConnectionHub> hub, int antennaId, AntennaCountsDTO counts, CancellationToken ct = default)
            => hub.Clients.Group(CrowdInfoAntennaConnectionHubMethods.AntennaGroup(antennaId))
                  .SendAsync(CrowdInfoAntennaConnectionHubMethods.ToClient.AntennaCountsUpdated, new { antennaId, counts }, ct);

        public static Task BroadcastEventCrowd(this IHubContext<CrowdInfoAntennaConnectionHub> hub, int eventId, EventAntennaCrowdDTO dto, CancellationToken ct = default)
            => hub.Clients.Group(CrowdInfoAntennaConnectionHubMethods.EventGroup(eventId))
                  .SendAsync(CrowdInfoAntennaConnectionHubMethods.ToClient.EventCrowdUpdated, dto, ct);
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.