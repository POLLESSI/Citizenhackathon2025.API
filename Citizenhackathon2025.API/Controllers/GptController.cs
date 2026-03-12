using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using HubEvents = CitizenHackathon2025.Contracts.Hubs.GptInteractionHubMethods;

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
        private readonly ILogger<GptController> _logger;

        public GptController(IGptInteractionRepository gptRepository, IHubContext<GPTHub> hubContext, IMistralAIService mistralAIService, ILogger<GptController> logger)
        {
            _gptRepository = gptRepository;
            _hubContext = hubContext;
            _mistralAIService = mistralAIService;
            _logger = logger;
        }

        //[HttpGet("all")]
        //public async Task<IActionResult> GetAllInteractions()
        //{
        //    var interactions = await _gptRepository.GetAllInteractionsAsync();
        //    // Secure null and avoid double mapping if the repo already returned DTOs
        //    var dtos = interactions?.Select(e => e.MapToGptInteractionDTO()).ToList() ?? new List<GptInteractionDTO>();
        //    return Ok(dtos);
        //}
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var interactions = await _gptRepository.GetAllInteractionsAsync();
            return Ok(interactions);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("The provided ID is invalid.");

            var interaction = await _gptRepository.GetByIdAsync(id);
            if (interaction == null)
                return NotFound($"Interaction with ID {id} not found.");

            return Ok(interaction.MapToGptInteractionDTO());
        }

        /// <summary>
        /// Sends a query to the AI ​​and returns an intelligent response.
        /// The response is also broadcast via SignalR to all connected clients.
        /// </summary>
        /// <param name="prompt">The text sent by the user.</param>
        /// <returns>An AI-generated response (simulated here).</returns>
        /// 
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

        //[Consumes("application/json")]
        [HttpPost("ask-mistral")]
        public async Task<IActionResult> AskMistral([FromBody] GptPromptRequest request)
        {
        #nullable disable
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 1. Call Mistral via Ollama
                var mistralResponse = await _mistralAIService.GenerateSuggestionAsync(
                    request.Prompt,
                    request.Latitude,
                    request.Longitude,
                    HttpContext.RequestAborted
                );

                // 2. Save the interaction
                var interaction = new GPTInteraction
                {
                    Prompt = request.Prompt,
                    Response = mistralResponse,
                    Active = true,
                    Model = "mistral",
                    Temperature = 0.7f,
                    SourceType = "MistralLocal"
                };

                var saved = await _gptRepository.UpsertInteractionAsync(interaction);

                // 3. Broadcast via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveGptResponse", new
                {
                    prompt = saved.Prompt,
                    response = saved.Response,
                    createdAt = saved.CreatedAt,
                    model = saved.Model
                });

                return Ok(new GptAnswerDTO
                {
                    Id = saved.Id,
                    Prompt = saved.Prompt,
                    Response = saved.Response,
                    CreatedAt = saved.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Mistral (Ollama)");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("test-gpt")]
        public async Task<IActionResult> TestGPT([FromServices] IAIRepository aiService)
        {
            try
            {
                var result = await aiService.AskChatGptAsync("Give me 3 ideas for activities on a rainy day in Han-Sur-Lesse.");
                return Ok(result);
            }
            catch (Exception ex)
            {

                throw;
            }
            
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
            try
            {
                var success = await _gptRepository.DeactivateInteractionAsync(id);
                if (!success)
                    return NotFound($"No active GPT interaction with ID {id} found.");

                return Ok(new { message = "Interaction deactivated successfully." });
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        [HttpPost("replay/{id}")]
        public async Task<IActionResult> ReplayInteraction(int id)
        {
            try
            {
                var interaction = await _gptRepository.GetByIdAsync(id);
                if (interaction == null)
                    return NotFound();

                await _hubContext.Clients.All.SendAsync("ReceiveGptResponse", new
                {
                    prompt = interaction.Prompt,
                    response = interaction.Response,
                    createdAt = interaction.CreatedAt
                });

                return Ok(new
                {
                    message = "Replayed successfully.",
                    interaction = interaction
                });
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
        [HttpPost("archive-expired")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ArchiveExpiredGptInteractions()
        {
            var archived = await _gptRepository.ArchivePastGptInteractionsAsync();
            return Ok(new { ArchivedCount = archived });
        }
    }
}

























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.

