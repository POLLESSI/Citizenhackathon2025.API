using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize(Policy = "Modo")]
    public sealed class ModerationHub : Hub
    {
    }
}