using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Infrastructure.Services
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

        public Task NotifyUserDeactivated(int id)
        {
            return _hubContext.Clients.All.SendAsync("UserDeactivated", id);
        }

        public Task NotifyUserRegistered(string email)
        {
            return _hubContext.Clients.All.SendAsync("UserRegistered", email);
        }

        public async Task NotifyUserUpdated(Users user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var dto = user.ToPublicDTO();

            await _hubContext.Clients.All.SendAsync("UserUpdated", dto);
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.