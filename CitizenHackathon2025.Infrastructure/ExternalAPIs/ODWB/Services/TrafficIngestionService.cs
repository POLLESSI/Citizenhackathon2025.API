using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Mappers;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Services
{
    public sealed class TrafficIngestionService : ITrafficIngestionService
    {
        private readonly IOdwbTrafficApiService _api;
        private readonly ITrafficConditionRepository _repo;
        private readonly ILogger<TrafficIngestionService> _log;

        public TrafficIngestionService(IOdwbTrafficApiService api, ITrafficConditionRepository repo, ILogger<TrafficIngestionService> log)
        {
            _api = api;
            _repo = repo;
            _log = log;
        }

        public async Task<int> PullAndUpsertAsync(OdwbQuery q, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var res = await _api.QueryAsync(q, ct);

            var mapped = res.Results
                .Select(r => OdwbTrafficMapper.TryMap(r, now))
                .Where(x => x is not null)
                .Cast<TrafficCondition>()
                .ToList();

            var ok = 0;
            foreach (var tc in mapped)
            {
                var saved = await _repo.UpsertTrafficConditionAsync(tc);
                if (saved is not null) ok++;
            }

            _log.LogInformation("ODWB ingestion upserted {Ok}/{Total}", ok, mapped.Count);
            return ok;
        }
    }

}
