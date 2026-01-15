using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces
{
    public interface ITrafficIngestionService
    {
        Task<int> PullAndUpsertAsync(OdwbQuery q, CancellationToken ct = default);
    }
}
