using CitizenHackathon2025.Domain.Entities.Telecom;

namespace CitizenHackathon2025.Application.Interfaces.ExternalProviders
{
    public interface ITelecomProviderClient
    {
        string ProviderName { get; }

        Task<TelecomAntennaSnapshot> GetAntennaSnapshotAsync(
            CancellationToken ct = default);
    }
}
