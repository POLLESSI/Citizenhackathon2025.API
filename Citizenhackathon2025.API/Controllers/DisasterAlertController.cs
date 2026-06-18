using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class DisasterAlertController : ControllerBase
    {
        private readonly ICriticalAlertQuorumService _quorum;
        private readonly IDisasterAlertRepository _repo;

        public DisasterAlertController(
            ICriticalAlertQuorumService quorum,
            IDisasterAlertRepository repo)
        {
            _quorum = quorum;
            _repo = repo;
        }

        [Authorize(Policy = Policies.UserPolicy)]
        [HttpPost("manual-critical-alert")]
        public async Task<ActionResult<DisasterAlertResultDTO>> ManualCriticalDisasterAlert(
            [FromBody] ManualDisasterAlertDTO request,
            CancellationToken ct)
        {

            if (request is null)
                return BadRequest("Request body is required.");

            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
                return BadRequest("Invalid coordinates.");

            var quorum = await _quorum.RegisterVoteAsync(
                CriticalAlertKind.Disaster,
                null,
                request.Latitude,
                request.Longitude,
                request.DeviceId,
                request.Description,
                ct);

            Console.WriteLine($"[DISASTER][DEVICE] {request.DeviceId}");
            Console.WriteLine($"[DISASTER][QUORUM] {quorum.ConfirmationCount}/{quorum.RequiredCount}");

            if (!quorum.Confirmed)
            {
                return Ok(new DisasterAlertResultDTO
                {
                    Ok = true,
                    Status = "Pending",
                    ConfirmationCount = quorum.ConfirmationCount,
                    RequiredCount = quorum.RequiredCount
                });
            }

            var nowUtc = DateTime.UtcNow;

            var alert = await _repo.InsertAsync(new DisasterAlert
            {
                DisasterType = (byte)request.DisasterType,
                Severity = request.Severity,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                PlaceName = request.PlaceName,
                Description = request.Description,
                ConfirmationCount = quorum.ConfirmationCount,
                RequiredCount = quorum.RequiredCount,
                Status = "Confirmed",
                ExpiresAtUtc = nowUtc.AddMinutes(10),
                Active = true
            }, ct);

            var payload = JsonSerializer.Serialize(new
            {
                Simulation = true,
                Message = "SIMULATION ONLY - No real emergency service contacted.",
                EmergencyNumbers = new[] { "112", "101", "100" },
                Alert = alert
            });

            var escalation = await _repo.InsertEscalationAsync(new EmergencyEscalationRequest
            {
                DisasterAlertId = alert.Id,
                TargetService = "Multi",
                Status = "PendingOperatorReview",
                PayloadJson = payload
            }, ct);

            return Ok(new DisasterAlertResultDTO
            {
                Ok = true,
                Status = "Confirmed",
                ConfirmationCount = quorum.ConfirmationCount,
                RequiredCount = quorum.RequiredCount,
                DisasterAlertId = alert.Id,
                EscalationRequestId = escalation.Id,
                ExpiresAtUtc = alert.ExpiresAtUtc
            });
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.