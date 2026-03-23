using CitizenHackathon2025.Application.Models;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IWallonieEnPocheSyncService
    {
        Task<SyncReport> SyncAsync(CancellationToken ct = default);
    }
}
