using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [ApiController]
    [Route("api/[controller]")]
    public class GptController : ControllerBase
    {
        private readonly IGptInteractionRepository _gptRepository;
        private readonly IGptOrchestrator _orchestrator;
        private readonly ILogger<GptController> _logger;

        public GptController(
            IGptInteractionRepository gptRepository,
            IGptOrchestrator orchestrator,
            ILogger<GptController> logger)
        {
            _gptRepository = gptRepository;
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var interactions = await _gptRepository.GetAllInteractionsAsync();
            var dtos = interactions?.Select(x => x.MapToGptInteractionDTO()).ToList() ?? new();
            return Ok(dtos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var interaction = await _gptRepository.GetByIdAsync(id);
            if (interaction == null)
                return NotFound($"Interaction with ID {id} not found.");

            return Ok(interaction.MapToGptInteractionDTO());
        }

        [HttpGet("status/{id:int}")]
        public async Task<IActionResult> GetStatus(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var interaction = await _gptRepository.GetByIdAsync(id);
            if (interaction == null)
                return NotFound();

            return Ok(new
            {
                interaction.Id,
                IsCompleted = !string.IsNullOrWhiteSpace(interaction.Response),
                interaction.Response,
                interaction.CreatedAt
            });
        }

        /// <summary>
        /// Asynchronous mode production/real customer.
        /// Returns 202 immediately, then the pipeline continues in the background.
        /// </summary>
        [HttpPost("ask-mistral")]
        public async Task<IActionResult> AskMistral([FromBody] GptPromptRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("The prompt cannot be empty.");

            var result = await _orchestrator.StartMistralRequestAsync(request, ct);
            return Accepted(result);
        }

        /// <summary>
        /// Special synchronous Swagger/debug mode.
        /// Wait for Ollama and return the final answer directly.
        /// </summary>
        [HttpPost("ask-mistral-sync")]
        public async Task<IActionResult> AskMistralSync( [FromBody] GptPromptRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("The prompt cannot be empty.");

            var result = await _orchestrator.RunMistralRequestAsync(request, ct);
            return Ok(result);
        }

        [HttpPost("cancel/{interactionId:int}")]
        public async Task<IActionResult> Cancel(int interactionId, [FromQuery] string? requestId = null)
        {
            if (interactionId <= 0)
                return BadRequest("The provided interaction ID is invalid.");

            try
            {
                var cancelled = await _orchestrator.CancelAsync(interactionId, requestId);

                if (!cancelled)
                {
                    return NotFound(new
                    {
                        message = "No active GPT request found for this interaction, or the requestId does not match.",
                        interactionId,
                        requestId
                    });
                }

                return Ok(new
                {
                    cancellationRequested = true,
                    interactionId,
                    requestId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling GPT request. InteractionId={InteractionId}", interactionId);
                return StatusCode(500, $"Error while cancelling request: {ex.Message}");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var success = await _gptRepository.DeactivateInteractionAsync(id);
            if (!success)
                return NotFound($"No active GPT interaction with ID {id} found.");

            return Ok(new { message = "Interaction deactivated successfully." });
        }

        [HttpPost("archive-expired")]
        [Authorize(Policy = Policies.AdminPolicy)]
        public async Task<IActionResult> ArchiveExpiredGptInteractions()
        {
            var archived = await _gptRepository.ArchivePastGptInteractionsAsync();
            return Ok(new { ArchivedCount = archived });
        }
    }
}

























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.

