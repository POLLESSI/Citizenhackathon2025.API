using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.DTOs.Requests;
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
        private readonly IMessageCorrelationService _correlator;
        private readonly IProfanityService _profanityService;
        private readonly IHubContext<MessageHub> _hub;
        private readonly IHubContext<ModerationHub> _moderationHub;

        public MessageController(
            IUserMessageService svc,
            IMessageCorrelationService correlator,
            IProfanityService profanityService,
            IHubContext<MessageHub> hub,
            IHubContext<ModerationHub> moderationHub)
        {
            _svc = svc;
            _correlator = correlator;
            _profanityService = profanityService;
            _hub = hub;
            _moderationHub = moderationHub;
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
        public async Task<IActionResult> Post([FromBody] CreateMessageRequest req, CancellationToken ct = default)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<MessageController>>();

            try
            {
                logger.LogInformation("POST /api/Message entered. User={User}", User.Identity?.Name);

                if (req is null)
                    return BadRequest("Body is required.");

                if (string.IsNullOrWhiteSpace(req.Content))
                    return BadRequest("Content is required.");

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var userId = User.Identity?.Name ?? "anon";
                logger.LogInformation("Creating raw message for user {UserId}", userId);

                var raw = new UserMessage
                {
                    UserId = userId,
                    Content = req.Content
                };

                logger.LogInformation("Before CorrelateAsync");
                var enriched = await _correlator.CorrelateAsync(raw, ct);

                logger.LogInformation("After CorrelateAsync. SourceType={SourceType}, SourceId={SourceId}, RelatedName={RelatedName}",
                    enriched.SourceType, enriched.SourceId, enriched.RelatedName);

                logger.LogInformation("Before AnalyzeAsync");
                var analysis = await _profanityService.AnalyzeAsync(req.Content, ct);

                logger.LogInformation("After AnalyzeAsync. HasProfanity={HasProfanity}, Score={Score}",
                    analysis.HasProfanity, analysis.Score);

                logger.LogInformation("Before InsertAsync");
                var saved = await _svc.InsertAsync(enriched, ct);

                logger.LogInformation("After InsertAsync. MessageId={MessageId}", saved.Id);

                if (analysis.HasProfanity)
                {
                    await _moderationHub.Clients.All.SendAsync(
                        "ReceiveProfanityDetected",
                        new ProfanityEventDto
                        {
                            MessageId = saved.Id,
                            ContentPreview = req.Content.Length > 120 ? req.Content[..120] : req.Content,
                            Score = analysis.Score,
                            ToxicityLevel = analysis.ToxicityLevel,
                            MatchedWords = analysis.MatchedWords,
                            OccurredAtUtc = DateTime.UtcNow
                        },
                        ct);
                }

                var dto = saved.MapToClientMessageDTO();

                logger.LogInformation("Before hub broadcast");
                await _hub.Clients.All.SendAsync("ReceiveMessageUpdate", dto, ct);
                logger.LogInformation("After hub broadcast");

                return Ok(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST /api/Message failed for user {User}. We aren't on X here.", User.Identity?.Name);

                return StatusCode(500, new
                {
                    message = "Failed to create message.",
                    detail = ex.Message,
                    type = ex.GetType().FullName
                });
            }
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