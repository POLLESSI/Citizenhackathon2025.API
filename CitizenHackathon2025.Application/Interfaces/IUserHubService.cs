namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUserHubService
    {
        Task BroadcastUserUpdatedAsync(CancellationToken cancellationToken = default);
    }
}
