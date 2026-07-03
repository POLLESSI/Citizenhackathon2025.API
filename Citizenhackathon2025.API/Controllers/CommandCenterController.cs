using CitizenHackathon2025.Application.Intelligence.CommandCenter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class CommandCenterController : ControllerBase
    {
        private readonly ICommandCenterService _commandCenter;

        public CommandCenterController(
            ICommandCenterService commandCenter)
        {
            _commandCenter = commandCenter;
        }

        /// <summary>
        /// Global snapshot of Wallonia.
        /// </summary>
        [HttpGet("snapshot")]
        public async Task<IActionResult> GetSnapshot(
            CancellationToken ct)
        {
            var snapshot =
                await _commandCenter.GetSnapshotAsync(ct);

            return Ok(snapshot);
        }

        /// <summary>
        /// Current active incidents.
        /// </summary>
        [HttpGet("incidents")]
        public async Task<IActionResult> GetIncidents(
            CancellationToken ct)
        {
            var incidents =
                await _commandCenter.GetActiveIncidentsAsync(ct);

            return Ok(incidents);
        }

        /// <summary>
        /// Highest risk zones.
        /// </summary>
        [HttpGet("risk-zones")]
        public async Task<IActionResult> GetRiskZones(
            CancellationToken ct)
        {
            var zones =
                await _commandCenter.GetRiskZonesAsync(ct);

            return Ok(zones);
        }

        /// <summary>
        /// Digital Twin status.
        /// </summary>
        [HttpGet("digital-twin")]
        public async Task<IActionResult> GetDigitalTwin(
            CancellationToken ct)
        {
            var twin =
                await _commandCenter.GetDigitalTwinAsync(ct);

            return Ok(twin);
        }
    }
}