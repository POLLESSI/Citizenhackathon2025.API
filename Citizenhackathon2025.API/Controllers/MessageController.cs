using CitizenHackathon2025.Domain.Entities; // UserMessage
using CitizenHackathon2025.Domain.Interfaces; // IUserMessageRepository
using CitizenHackathon2025.Application.Interfaces; // IMessageCorrelationService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.DTOs.DTOs; 
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IUserMessageRepository _repo;
        private readonly IMessageCorrelationService _correlator;
        private readonly IHubContext<MessageHub> _hub;

        public MessageController(
            IUserMessageRepository repo,
            IMessageCorrelationService correlator,
            IHubContext<MessageHub> hub)
        {
            _repo = repo;
            _correlator = correlator;
            _hub = hub;
        }

        [HttpGet("latest")]
        [Authorize(Policy = "User")]
        public async Task<IActionResult> GetLatest([FromQuery] int take = 100, CancellationToken ct = default)
        {
            var list = await _repo.GetLatestAsync(take, ct);
            var dtos = list.Select(ToDto).ToList();
            return Ok(dtos);
        }

        public class CreateMessageRequest
        {
            public string Content { get; set; } = "";
        }

        [HttpPost]
        [Authorize(Policy = "User")]
        public async Task<IActionResult> Post([FromBody] CreateMessageRequest req, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest("Content cannot be empty.");

            var userId = User.Identity?.Name ?? "anon";

            var raw = new UserMessage
            {
                UserId = userId,
                Content = req.Content
            };

            // Corrélation
            var enriched = await _correlator.CorrelateAsync(raw, ct);

            // Insert
            var saved = await _repo.InsertAsync(enriched, ct);
            var dto = ToDto(saved);

            // Broadcast via SignalR
            await _hub.Clients.All.SendAsync("ReceiveMessageUpdate", dto, ct);

            return Ok(dto);
        }

        private static ClientMessageDTO ToDto(UserMessage m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            SourceType = m.SourceType,
            SourceId = m.SourceId,
            RelatedName = m.RelatedName,
            Latitude = m.Latitude.HasValue ? (double?)m.Latitude.Value : null,
            Longitude = m.Longitude.HasValue ? (double?)m.Longitude.Value : null,
            Tags = m.Tags,
            Content = m.Content,
            CreatedAt = m.CreatedAt
        };
    }
}
