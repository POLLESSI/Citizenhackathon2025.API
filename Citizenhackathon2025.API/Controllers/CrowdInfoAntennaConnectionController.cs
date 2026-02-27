using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
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
        [HttpPost("ping")]
        public async Task<IActionResult> Ping([FromBody] PingAntennaRequest req, CancellationToken ct)
        {
            static byte[] FromB64(string s)
            {
                try { return Convert.FromBase64String(s); }
                catch { throw new ArgumentException("Invalid base64."); }
            }

            var deviceHash = FromB64(req.DeviceHashBase64);
            var ipHash = string.IsNullOrWhiteSpace(req.IpHashBase64) ? null : FromB64(req.IpHashBase64);
            var macHash = string.IsNullOrWhiteSpace(req.MacHashBase64) ? null : FromB64(req.MacHashBase64);

            await _svc.PingAsync(req.AntennaId, deviceHash, ipHash, macHash, req.Source, req.SignalStrength, req.Band, req.AdditionalJson, ct);
            return Ok();
        }
        // GET api/crowdinfoantennaconnection/deleted?antennaId=1&since=2026-02-01T00:00:00Z&take=100&cursorDeletedId=12345
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
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.