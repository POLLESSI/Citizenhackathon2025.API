using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.DTOs.DTOs.Antennas;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class CrowdInfoAntennaConnectionController : ControllerBase
    {
        private readonly ICrowdInfoAntennaConnectionService _svc;
        public CrowdInfoAntennaConnectionController(ICrowdInfoAntennaConnectionService svc) => _svc = svc;

        // POST api/crowdinfoantennaconnection/ping
        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpPost("ping")]
        public async Task<IActionResult> Ping([FromBody] PingAntennaRequest req, CancellationToken ct)
        {
            if (req is null)
                return BadRequest("Request body is required.");

            if (req.AntennaId <= 0)
                return BadRequest("AntennaId must be > 0.");

            if (!TryDecodeHash32(req.DeviceHashBase64, nameof(req.DeviceHashBase64), required: true, out var deviceHash, out var error))
                return BadRequest(error);

            if (!TryDecodeHash32(req.IpHashBase64, nameof(req.IpHashBase64), required: false, out var ipHash, out error))
                return BadRequest(error);

            if (!TryDecodeHash32(req.MacHashBase64, nameof(req.MacHashBase64), required: false, out var macHash, out error))
                return BadRequest(error);

            var eventId = req.EventId <= 0 ? null : req.EventId;

            await _svc.PingAsync(
                req.AntennaId,
                eventId,
                deviceHash!,
                ipHash,
                macHash,
                req.Source,
                req.SignalStrength,
                string.IsNullOrWhiteSpace(req.Band) || req.Band == "string" ? null : req.Band.Trim(),
                string.IsNullOrWhiteSpace(req.AdditionalJson) || req.AdditionalJson == "string" ? null : req.AdditionalJson,
                ct);

            return Ok(new
            {
                req.AntennaId,
                EventId = eventId,
                Status = "Ping accepted",
                SeenAtUtc = DateTime.UtcNow
            });
        }

        private static bool TryDecodeHash32(string? value, string fieldName, bool required, out byte[]? bytes, out string? error)
        {
            bytes = null;
            error = null;

            if (string.IsNullOrWhiteSpace(value) || value == "string")
            {
                if (required)
                {
                    error = $"{fieldName} is required and must be a valid Base64 string representing exactly 32 bytes.";
                    return false;
                }

                return true;
            }

            try
            {
                bytes = Convert.FromBase64String(value.Trim());
            }
            catch (FormatException)
            {
                error = $"{fieldName} must be valid Base64.";
                return false;
            }

            if (bytes.Length != 32)
            {
                error = $"{fieldName} must decode to exactly 32 bytes. Actual length: {bytes.Length}.";
                return false;
            }

            return true;
        }
        // GET api/crowdinfoantennaconnection/deleted?antennaId=1&since=2026-02-01T00:00:00Z&take=100&cursorDeletedId=12345
        //[Authorize(Policy = Policies.AdminPolicy)]
        //[Authorize(Policy = Policies.ModoPolicy)]
        //[HttpGet("deleted")]
        //public async Task<IActionResult> GetDeleted(
        //    [FromQuery] int antennaId,
        //    [FromQuery] string since,
        //    [FromQuery] int take = 100,
        //    [FromQuery] long? cursorDeletedId = null,
        //    CancellationToken ct = default)
        //{
        //    if (antennaId <= 0) return BadRequest("antennaId must be > 0.");
        //    if (string.IsNullOrWhiteSpace(since)) return BadRequest("since is required (ISO 8601, UTC).");

        //    // ISO 8601 strict, ex: 2026-02-01T00:00:00Z
        //    if (!DateTime.TryParse(
        //            since,
        //            CultureInfo.InvariantCulture,
        //            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
        //            out var sinceUtc))
        //    {
        //        return BadRequest("since must be ISO 8601 UTC (example: 2026-02-01T00:00:00Z).");
        //    }

        //    var rows = await _svc.GetDeletedAsync(antennaId, sinceUtc, take, cursorDeletedId, ct);

        //    // Cursor de pagination: reprend le dernier DeletedId renvoyé
        //    var nextCursor = rows.Count > 0 ? rows[^1].DeletedId : (long?)null;

        //    return Ok(new
        //    {
        //        antennaId,
        //        sinceUtc,
        //        take = Math.Clamp(take, 1, 500),
        //        cursorDeletedId,
        //        nextCursorDeletedId = nextCursor,
        //        items = rows
        //    });
        //}
        //[Authorize(Policy = Policies.AdminPolicy)]
        [AllowAnonymous]
        [HttpPost("simulate")]
        public async Task<IActionResult> Simulate([FromBody] SimulateAntennaConnectionsRequest request, [FromServices] IAntennaSimulationService simulator, CancellationToken ct)
        {
            try
            {
                if (request.AntennaId <= 0)
                    return BadRequest("AntennaId must be > 0.");

                if (request.DeviceCount is < 1 or > 10_000)
                    return BadRequest("DeviceCount must be between 1 and 10000.");

                await simulator.SimulateAsync(request, ct);

                return Ok(new
                {
                    request.AntennaId,
                    request.EventId,
                    request.DeviceCount,
                    request.DurationSeconds,
                    request.BurstMode,
                    SimulatedAtUtc = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Simulation failed",
                    detail: ex.ToString(),
                    statusCode: 500);
            }
        }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.