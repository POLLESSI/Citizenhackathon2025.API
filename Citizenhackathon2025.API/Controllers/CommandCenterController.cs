using CitizenHackathon2025.Application.Intelligence.CommandCenter;
using CitizenHackathon2025.Application.Intelligence.Prediction;
using CitizenHackathon2025.Application.Intelligence.Decision;
using CitizenHackathon2025.Application.Intelligence.Replay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOrModo")]
    public sealed class CommandCenterController : ControllerBase
    {
        private readonly ICommandCenterService _commandCenter;
        private readonly IPredictionEngine _prediction;
        private readonly IReplayService _replay;
        private readonly IDecisionEngine _decision;

        public CommandCenterController(ICommandCenterService commandCenter, IPredictionEngine prediction, IReplayService replay, IDecisionEngine decision)
        {
            _commandCenter = commandCenter;
            _prediction = prediction;
            _replay = replay;
            _decision = decision;
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
        [HttpGet("predictions")]
        public async Task<IActionResult> GetPredictions(CancellationToken ct)
        {
            var zones = await _commandCenter.GetRiskZonesAsync(ct);
            var predictions = await _prediction.PredictAsync(zones, ct);

            return Ok(predictions);
        }

        [HttpGet("replay")]
        public async Task<IActionResult> GetReplay([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
        {
            if (fromUtc is null)
                return BadRequest("fromUtc is required. Example: 2026-07-06T00:00:00Z");

            if (toUtc is null)
                return BadRequest("toUtc is required. Example: 2026-07-06T23:59:59Z");

            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            var to = DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);

            if (from >= to)
                return BadRequest("fromUtc must be earlier than toUtc.");

            var frames = await _replay.GetFramesAsync(from, to, ct);
            return Ok(frames);
        }
        [HttpGet("actions")]
        public async Task<IActionResult> GetDecisionActions(CancellationToken ct)
        {
            var incidents = await _commandCenter.GetActiveIncidentsAsync(ct);
            var actions = await _decision.RecommendActionsAsync(incidents, ct);

            return Ok(actions);
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.