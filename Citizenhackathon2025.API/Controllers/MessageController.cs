using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public sealed class MessageController : ControllerBase
    {
        private readonly IUserMessageService _svc;
        private readonly IHubContext<MessageHub> _hub;

        public MessageController(IUserMessageService svc, IHubContext<MessageHub> hub)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest([FromQuery] int take = 100, CancellationToken ct = default)
        {
            var list = await _svc.GetLatestAsync(take, ct);
            var dtos = list.MapToClientMessageDTOs();

            return Ok(dtos);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            var msg = await _svc.GetByIdAsync(id, ct);

            if (msg is null)
                return NotFound();

            return Ok(msg.MapToClientMessageDTO());
        }


        [HttpPost]
        [Authorize(Policy = Policies.UserPolicy)]
        public async Task<ActionResult<ClientMessageDTO>> Create([FromBody] CreateMessageRequest req, CancellationToken ct)
        {
            if (req is null)
            {
                return BadRequest("Body is required.");
            }

            if (string.IsNullOrWhiteSpace(req.Content))
            {
                return BadRequest("Content is required.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }


            var message =
                new UserMessage
                {
                    UserId = User.Identity?.Name ?? "anon",
                    Content = req.Content.Trim(),
                    SourceType = string.IsNullOrWhiteSpace(req.SourceType) ? "Other" : req.SourceType.Trim(),
                    SourceId = req.RelatedId,
                    RelatedName = string.IsNullOrWhiteSpace(req.RelatedName) ? null : req.RelatedName.Trim(),
                    Latitude = req.Latitude.HasValue ? (decimal?) req.Latitude.Value : null,
                    Longitude = req.Longitude.HasValue ? (decimal?) req.Longitude.Value : null,
                    CreatedAt = DateTime.UtcNow,
                    Active = true
                };

            var saved = await _svc.InsertAsync(message, req.RequestAdminReview, ct);
            var dto = saved.MapToClientMessageDTO();

            await _hub.Clients.All.SendAsync("ReceiveMessageUpdate", dto, ct);

            return Ok(dto);
        }


        [HttpDelete("{id:int}")]
        [Authorize(Policy = Policies.ModoPolicy)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
        {
            var ok = await _svc.DeleteMessageAsync(id, ct);

            if (!ok)
                return NotFound();


            await _hub.Clients.All.SendAsync("ReceiveMessageDeleted", new { Id = id }, ct);


            return NoContent();
        }
    }
}




















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.