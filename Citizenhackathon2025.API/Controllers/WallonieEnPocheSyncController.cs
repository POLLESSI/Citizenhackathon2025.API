using CitizenHackathon2025.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class WallonieEnPocheSyncController : ControllerBase
    {
        private readonly IWallonieEnPocheSyncService _syncService;

        public WallonieEnPocheSyncController(IWallonieEnPocheSyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run(CancellationToken ct)
        {
            var report = await _syncService.SyncAsync(ct);
            return Ok(report);
        }
    }
}
































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.