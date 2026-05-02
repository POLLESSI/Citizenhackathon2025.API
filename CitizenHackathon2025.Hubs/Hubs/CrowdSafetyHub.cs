using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize(Policy = "CrowdSafetyPolicy")]
    public sealed class CrowdSafetyHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                CrowdSafetyHubMethods.AuthorizedGroup);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                CrowdSafetyHubMethods.AuthorizedGroup);

            await base.OnDisconnectedAsync(exception);
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.