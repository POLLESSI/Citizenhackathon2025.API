namespace CitizenHackathon2025.Application.Interfaces.ExternalProviders
{
    public interface IMongoConnectionStringProvider
    {
        Task<string> GetConnectionStringAsync(CancellationToken ct = default);
    }
}
