using Citizenhackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUserHubService
    {
        Task BroadcastUserUpdatedAsync(CancellationToken cancellationToken = default);
        Task NotifyUserRegistered(string email); // existante ?
        Task NotifyUserUpdated(User user);
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.