using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Builders;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Infrastructure.ExternalProviders.Common;
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

            _log.LogInformation("ODWB GET host={Host} path={Path}", uri.Host, uri.AbsolutePath);

            using var resp = await _http.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            _log.LogInformation("ODWB status {Status}", (int)resp.StatusCode);

            if (!resp.IsSuccessStatusCode)
            {
                var safeBody = await ReadSmallBodyAsync(resp, 300, ct);

                _log.LogWarning(
                    "ODWB call failed status={Status} host={Host} body={Body}",
                    (int)resp.StatusCode,
                    uri.Host,
                    safeBody);

                resp.EnsureSuccessStatusCode();
            }

            if (!_opt.BaseUrl.Contains("/records", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"ODWB BaseUrl invalid. Expected records endpoint, got: {_opt.BaseUrl}");
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);

            var data = await JsonSerializer.DeserializeAsync<OdwbRecordsResponse<OdwbDynamicRecord>>(
                stream,
                StrictExternalJson.Options,
                ct);

            return data ?? new OdwbRecordsResponse<OdwbDynamicRecord>();
        }

        private static async Task<string> ReadSmallBodyAsync(
            HttpResponseMessage response,
            int max,
            CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            if (body.Length <= max)
                return body;

            return body[..max] + "…";
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.