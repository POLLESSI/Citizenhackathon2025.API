using System.Buffers.Text;
using System.Text;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Mvc;

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
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.