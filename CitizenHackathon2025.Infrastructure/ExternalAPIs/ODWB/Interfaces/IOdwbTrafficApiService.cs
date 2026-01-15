using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Wrap;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces
{
    public interface IOdwbTrafficApiService
    {
        Task<OdwbRecordsResponse<OdwbDynamicRecord>> QueryAsync(OdwbQuery query, CancellationToken ct = default);
    }
}
