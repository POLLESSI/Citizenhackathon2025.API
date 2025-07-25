using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize] // optional if verifying the token manually
    public class OutZenHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Log or special management at connection
            await base.OnConnectedAsync();
        }
    }
}
































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.