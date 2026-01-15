using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
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
        private readonly IMessageCorrelationService _correlator;
        private readonly IHubContext<MessageHub> _hub;

        public MessageController(
            IUserMessageService svc,
            IMessageCorrelationService correlator,
            IHubContext<MessageHub> hub)
        {
            _svc = svc;
            _correlator = correlator;
            _hub = hub;
        }

        // GET api/message/latest?take=100
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest([FromQuery] int take = 100, CancellationToken ct = default)
        {
            var list = await _svc.GetLatestAsync(take, ct);
            var dtos = list.MapToClientMessageDTOs(); // <-- extension
            return Ok(dtos);
        }
        // GET api/message/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            var msg = await _svc.GetByIdAsync(id, ct);
            if (msg is null) return NotFound();

            return Ok(msg.MapToClientMessageDTO()); // <-- extension
        }

        public sealed class CreateMessageRequest
        {
            public string Content { get; set; } = "";
        }

        // POST api/message
        [HttpPost]
        [Authorize(Policy = "User")]
        public async Task<IActionResult> Post([FromBody] CreateMessageRequest req, CancellationToken ct = default)
        {
            if (req is null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest("Content cannot be empty.");

            var userId = User.Identity?.Name ?? "anon";

            var raw = new UserMessage
            {
                UserId = userId,
                Content = req.Content
            };

            // Correlation (SourceType, SourceId, RelatedName, Tags, Lat/Lng...)
            var enriched = await _correlator.CorrelateAsync(raw, ct);

            // Insert via Service (validation + normalisation + repo)
            var saved = await _svc.InsertAsync(enriched, ct);

            var dto = saved.MapToClientMessageDTO(); 
            await _hub.Clients.All.SendAsync("ReceiveMessageUpdate", dto, ct);

            return Ok(dto);
        }

        // DELETE api/message/{id}
        // Soft-delete via trigger INSTEAD OF DELETE (repo executes DELETE)
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "User")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
        {
            var ok = await _svc.DeleteMessageAsync(id, ct);
            if (!ok) return NotFound();

            // Optional: notify customers (if your UI needs to remove the message)
            await _hub.Clients.All.SendAsync("ReceiveMessageDeleted", new { Id = id }, ct);

            return NoContent();
        }

        
    }
}




















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.