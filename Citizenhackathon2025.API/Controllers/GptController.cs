using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [ApiController]
    [Route("api/[controller]")]
    public class GptController : ControllerBase
    {
        private readonly IGptInteractionRepository _gptRepository;
        private readonly IHubContext<GPTHub> _hubContext;
        private readonly IMistralAIService _mistralAIService;
        private readonly IGptRequestRegistry _gptRequestRegistry;
        private readonly ILogger<GptController> _logger;

        public GptController(
            IGptInteractionRepository gptRepository,
            IHubContext<GPTHub> hubContext,
            IMistralAIService mistralAIService,
            IGptRequestRegistry gptRequestRegistry,
            ILogger<GptController> logger)
        {
            _gptRepository = gptRepository;
            _hubContext = hubContext;
            _mistralAIService = mistralAIService;
            _gptRequestRegistry = gptRequestRegistry;
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

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string question)
        {
            var response = $"(Simulated GPT Response) You asked : \"{question}\"";

            var saved = await _gptRepository.UpsertInteractionAsync(new GPTInteraction
            {
                Prompt = question,
                Response = response,
                Active = true
            });

            var dto = new GptAnswerDTO
            {
                Id = saved?.Id,
                Prompt = saved?.Prompt ?? question,
                Response = saved?.Response ?? response,
                CreatedAt = saved?.CreatedAt ?? DateTime.UtcNow
            };

            return Ok(dto);
        }

        [HttpPost("ask-mistral")]
        public async Task<IActionResult> AskMistral([FromBody] GptPromptRequest request)
        {
            _logger.LogInformation("=== ASK MISTRAL STREAM ENTER ===");
            _logger.LogInformation("Prompt={Prompt}", request?.Prompt);
            _logger.LogInformation("Lat={Lat}, Lng={Lng}", request?.Latitude, request?.Longitude);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("The prompt cannot be empty.");

            GPTInteraction? saved = null;
            CancellationTokenSource? linkedCts = null;
            string? requestId = null;

            try
            {
                saved = await _gptRepository.UpsertInteractionAsync(new GPTInteraction
                {
                    Prompt = request.Prompt.Trim(),
                    Response = string.Empty,
                    Active = true,
                    Model = "mistral",
                    Temperature = 0.7f,
                    SourceType = "MistralLocal"
                });

                if (saved is null || saved.Id <= 0)
                    return StatusCode(500, "Unable to create GPT interaction.");

                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
                requestId = _gptRequestRegistry.Register(saved.Id, linkedCts);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStarted",
                    saved.Id,
                    requestId,
                    linkedCts.Token);

                var finalResponse = await _mistralAIService.StreamSuggestionAsync(
                    request.Prompt,
                    request.Latitude,
                    request.Longitude,
                    async chunk =>
                    {
                        chunk.InteractionId = saved.Id;
                        chunk.RequestId = requestId!;

                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveGptResponseChunk",
                            chunk.InteractionId,
                            chunk.RequestId,
                            chunk.Chunk,
                            false,
                            linkedCts.Token);
                    },
                    linkedCts.Token);

                saved.Response = finalResponse;

                var updated = await _gptRepository.UpsertInteractionAsync(saved);

                var finalDto = new GptInteractionDTO
                {
                    Id = updated?.Id ?? saved.Id,
                    Prompt = updated?.Prompt ?? saved.Prompt,
                    Response = updated?.Response ?? finalResponse,
                    CreatedAt = updated?.CreatedAt != default ? updated.CreatedAt : DateTime.UtcNow};

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseChunk",
                    finalDto.Id,
                    requestId!,
                    string.Empty,
                    true,
                    linkedCts.Token);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponse",
                    finalDto,
                    linkedCts.Token);

                return Ok(finalDto);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "GPT generation cancelled. InteractionId={InteractionId}, RequestId={RequestId}",
                    saved?.Id,
                    requestId);

                if (saved is not null && saved.Id > 0)
                {
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveGptResponseCancelled",
                        saved.Id,
                        requestId ?? string.Empty,
                        CancellationToken.None);
                }

                return Ok(new
                {
                    cancelled = true,
                    interactionId = saved?.Id,
                    requestId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming Mistral response.");

                if (saved is not null && saved.Id > 0)
                {
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveGptResponseFailed",
                        saved.Id,
                        ex.Message,
                        CancellationToken.None);
                }

                return StatusCode(500, $"Error: {ex.Message}");
            }
            finally
            {
                if (saved is not null && saved.Id > 0)
                    _gptRequestRegistry.Remove(saved.Id);

                linkedCts?.Dispose();
            }
        }

        [HttpPost("cancel/{interactionId:int}")]
        public IActionResult Cancel(int interactionId, [FromQuery] string? requestId = null)
        {
            if (interactionId <= 0)
                return BadRequest("The provided interaction ID is invalid.");

            try
            {
                var cancelled = _gptRequestRegistry.TryCancel(interactionId, requestId);

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

        [HttpGet("test-gpt")]
        public async Task<IActionResult> TestGPT([FromServices] IAIRepository aiService)
        {
            var result = await aiService.AskChatGptAsync("Give me 3 ideas for activities on a rainy day in Han-Sur-Lesse.");
            return Ok(result);
        }

        [HttpPost("suggest")]
        public async Task<IActionResult> SuggestAlternative([FromBody] GptPromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("The prompt cannot be empty.");

            try
            {
                var response = await _mistralAIService.GenerateSuggestionAsync(
                    request.Prompt,
                    request.Latitude,
                    request.Longitude);

                return Ok(new { Suggestion = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating suggestion");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var success = await _gptRepository.DeactivateInteractionAsync(id);
            if (!success)
                return NotFound($"No active GPT interaction with ID {id} found.");

            return Ok(new { message = "Interaction deactivated successfully." });
        }

        [HttpPost("replay/{id}")]
        public async Task<IActionResult> ReplayInteraction(int id)
        {
            var interaction = await _gptRepository.GetByIdAsync(id);
            if (interaction == null)
                return NotFound();

            await _hubContext.Clients.All.SendAsync("ReceiveGptResponse", new
            {
                Id = interaction.Id,
                prompt = interaction.Prompt,
                response = interaction.Response,
                createdAt = interaction.CreatedAt
            });

            return Ok(new
            {
                message = "Replayed successfully.",
                interaction
            });
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

