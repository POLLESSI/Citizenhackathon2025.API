using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Builders;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Wrap;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Services
{
   public sealed class OdwbTrafficApiService : IOdwbTrafficApiService
    {
        private readonly HttpClient _http;
        private readonly TrafficApiOptions _opt;
        private readonly ILogger<OdwbTrafficApiService> _log;
        
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public OdwbTrafficApiService( HttpClient http, ILogger<OdwbTrafficApiService> log, IOptions<TrafficApiOptions> opt)
            => (_http, _log, _opt) = (http, log, opt.Value);

        public async Task<OdwbRecordsResponse<OdwbDynamicRecord>> QueryAsync(OdwbQuery query, CancellationToken ct = default)
        {
            var q = query with { Limit = query.Limit ?? _opt.DefaultLimit };
            var uri = OdwbUrlBuilder.Build(_opt.BaseUrl, q);

            _log.LogInformation("ODWB GET {Uri}", uri);
            var resp = await _http.GetAsync(uri, ct);
            _log.LogInformation("ODWB status {Status}", (int)resp.StatusCode);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _log.LogWarning("ODWB call failed {Status} {Uri} body={Body}", (int)resp.StatusCode, uri, body);
                resp.EnsureSuccessStatusCode();
            }

            var data = await resp.Content.ReadFromJsonAsync<OdwbRecordsResponse<OdwbDynamicRecord>>(JsonOpts, ct);
            return data ?? new OdwbRecordsResponse<OdwbDynamicRecord>();
        }
    }
}
