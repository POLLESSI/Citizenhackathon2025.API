using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]

    [Route("api/admin/message-triage")]

    [Authorize(Policy = "AdminOnly")]
    public sealed class AdminMessageTriageController : ControllerBase
    {
        private readonly IUserMessageAdminQueueRepository _repository;
        private readonly ILogger<AdminMessageTriageController> _logger;

        public AdminMessageTriageController(IUserMessageAdminQueueRepository repository, ILogger<AdminMessageTriageController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==========================================
        // GET /api/admin/message-triage
        // ==========================================

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AdminMessageQueueDto>>> GetAsync([FromQuery] string? status, [FromQuery] string? category, [FromQuery] byte? priority, [FromQuery] int take = 200, CancellationToken ct = default)
        {
            var filter =
                new AdminMessageQueueFilter
                {
                    Status = status,
                    Category = category,
                    Priority = priority,
                    Take = Math.Clamp(take, 1, 500)
                };


            var items = await _repository.GetAsync(filter, ct);


            _logger.LogInformation(
                "[ADMIN-MESSAGE-TRIAGE] " +
                "Loaded {Count} report(s). " +
                "Status={Status}; " +
                "Category={Category}; " +
                "Priority={Priority}",
                items.Count,
                status,
                category,
                priority);


            return Ok(items);
        }


        // ==========================================
        // PUT /api/admin/message-triage/{id}/status
        // ==========================================

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatusAsync([FromRoute] int id,[FromBody] UpdateAdminMessageStatusRequest request, CancellationToken ct = default)
        {
            if (id <= 0)
                return BadRequest();

            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (!Enum.TryParse<AdminMessageStatus>(request.Status, ignoreCase: true, out var status))
            {
                return BadRequest($"Unknown status '{request.Status}'.");
            }

            /*
             * When an administrator starts reviewing
             * or accepts a report, assign it to the
             * current administrator unless another
             * assignment was explicitly supplied.
             */
            var assignedTo = request.AssignedTo;

            if (string.IsNullOrWhiteSpace(assignedTo) && status is AdminMessageStatus.Reviewing or AdminMessageStatus.Accepted)
            {
                assignedTo = User.Identity?.Name;
            }


            var updated = await _repository.UpdateStatusAsync(id, status, request.AdminNote, assignedTo, ct);


            if (!updated)
                return NotFound();


            _logger.LogInformation(
                "[ADMIN-MESSAGE-TRIAGE] " +
                "QueueId={QueueId} updated to {Status} " +
                "by {Admin}",
                id,
                status,
                User.Identity?.Name);


            return NoContent();
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.
