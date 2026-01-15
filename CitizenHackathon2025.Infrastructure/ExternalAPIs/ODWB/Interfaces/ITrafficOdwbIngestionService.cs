namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces
{
    public interface ITrafficOdwbIngestionService
    {
        Task<int> SyncAsync(int? limit = null, CancellationToken ct = default);
    }
}
