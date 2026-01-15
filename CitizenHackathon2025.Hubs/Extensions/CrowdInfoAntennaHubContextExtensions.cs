using System.Threading;
using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class CrowdInfoAntennaHubContextExtensions
    {
        public static Task BroadcastAntennaUpserted(this IHubContext<CrowdInfoAntennaHub> hub, CrowdInfoAntennaDTO antenna, CancellationToken ct = default)
            => hub.Clients.All.SendAsync(CrowdInfoAntennaHubMethods.ToClient.AntennaUpserted, antenna, ct);

        public static Task BroadcastAntennaUpsertedToAntennaGroup(this IHubContext<CrowdInfoAntennaHub> hub, int antennaId, CrowdInfoAntennaDTO antenna, CancellationToken ct = default)
            => hub.Clients.Group(CrowdInfoAntennaHubMethods.AntennaGroup(antennaId))
                  .SendAsync(CrowdInfoAntennaHubMethods.ToClient.AntennaUpserted, antenna, ct);

        public static Task BroadcastAntennaArchived(this IHubContext<CrowdInfoAntennaHub> hub, int antennaId, CancellationToken ct = default)
            => hub.Clients.All.SendAsync(CrowdInfoAntennaHubMethods.ToClient.AntennaArchived, antennaId, ct);
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.