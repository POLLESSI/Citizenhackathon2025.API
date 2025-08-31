using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class UserHubService : CitizenHackathon2025.Application.Interfaces.IUserHubService
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
            throw new NotImplementedException();
        }

        public async Task NotifyUserRegistered(string email)
        {
            await _hubContext.Clients.All.SendAsync("UserRegistered", email);
        }

        public async Task NotifyUserUpdated(Users user)
        {
            await _hubContext.Clients.All.SendAsync("UserUpdated", new
            {
                user.Id,
                user.Email,
                user.PasswordHash,
                user.Role,
                user.Status
            });
        }
    } 
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.