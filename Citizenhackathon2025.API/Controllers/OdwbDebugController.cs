using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Infrastructure.Helpers;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/odwb")]
    public sealed class OdwbDebugController : ControllerBase
    {
        private readonly IOdwbTrafficApiService _odwb;
        private readonly ITrafficConditionRepository _repo;
        private readonly ILogger<OdwbDebugController> _log;

        public OdwbDebugController(
            IOdwbTrafficApiService odwb,
            ITrafficConditionRepository repo,
            ILogger<OdwbDebugController> log)
            => (_odwb, _repo, _log) = (odwb, repo, log);

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("ping")]
        public async Task<IActionResult> Ping(CancellationToken ct)
        {
            var res = await _odwb.QueryAsync(new OdwbQuery(Limit: 1), ct);
            return Ok(new { Count = res?.Results?.Count ?? 0 });
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("odwb/test")]
        public async Task<IActionResult> TestOdwb(CancellationToken ct)
        {
            var r = await _odwb.QueryAsync(new OdwbQuery(Limit: 1), ct);
            return Ok(r);
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("debug-raw")]
        public async Task<IActionResult> DebugRaw([FromServices] IHttpClientFactory factory, [FromServices] IConfiguration config, [FromQuery] int limit = 5, CancellationToken ct = default)
        {
            var baseUrl = config["ExternalProviders:ODWB:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                return Problem("ExternalProviders:ODWB:BaseUrl is missing.");

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                return BadRequest($"Invalid ODWB BaseUrl: {baseUrl}");

            var separator = baseUrl.Contains('?') ? "&" : "?";
            var url = $"{baseUrl.TrimEnd('/')}{separator}limit={Math.Clamp(limit, 1, 100)}";

            try
            {
                var http = factory.CreateClient("ODWB");

                using var response = await http.GetAsync(url, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                return StatusCode((int)response.StatusCode, new
                {
                    Url = url,
                    StatusCode = (int)response.StatusCode,
                    response.ReasonPhrase,
                    Body = body
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new
                {
                    Error = "ODWB HTTP call failed.",
                    Url = url,
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        private static string? TryString(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static int? TryInt(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var i) ? i : null;

        private static decimal? TryGeoLat(Dictionary<string, object?> r)
        {
            if (!r.TryGetValue("geo_point_2d", out var v) || v is null) return null;

            if (v is JsonElement je && je.ValueKind == JsonValueKind.Object)
                if (je.TryGetProperty("lat", out var lat) && lat.TryGetDecimal(out var d)) return d;

            return null;
        }

        private static decimal? TryGeoLon(Dictionary<string, object?> r)
        {
            if (!r.TryGetValue("geo_point_2d", out var v) || v is null) return null;

            if (v is JsonElement je && je.ValueKind == JsonValueKind.Object)
                if (je.TryGetProperty("lon", out var lon) && lon.TryGetDecimal(out var d)) return d;

            return null;
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.