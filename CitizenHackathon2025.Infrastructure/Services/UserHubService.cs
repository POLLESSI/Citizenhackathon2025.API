using Microsoft.AspNetCore.SignalR;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Application.Services
{
    public class UserHubService : IUserHubService
    {
        private readonly IHubContext<UserHub> _hubContext;

        public UserHubService(IHubContext<UserHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastUserUpdatedAsync(CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.All.SendAsync("UserUpdated", cancellationToken);
        }
    } 
}
