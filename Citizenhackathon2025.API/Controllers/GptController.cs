using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using HubEvents = CitizenHackathon2025.Contracts.Hubs.GptInteractionHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class GptController : ControllerBase
    {
        private readonly IGPTRepository _gptRepository;
        private readonly IHubContext<GPTHub> _hubContext;

        public GptController(IGPTRepository gptRepository, IHubContext<GPTHub> hubContext)
        {
            _gptRepository = gptRepository;
            _hubContext = hubContext;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllInteractions()
        {
            var interactions = await _gptRepository.GetAllInteractionsAsync();
            // Secure null and avoid double mapping if the repo already returned DTOs
            var dtos = interactions?.Select(e => e.MapToGptInteractionDTO()).ToList() ?? new List<GptInteractionDTO>();
            return Ok(dtos);
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

        [HttpPost("ask-gpt")]
        [Consumes("application/json")]
        public async Task<IActionResult> AskGpt([FromBody] GptPrompt prompt)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(prompt.Prompt))
                return BadRequest("Prompt cannot be empty");

            var generatedResponse = $"[Simulated GPT] Response to: \"{prompt.Prompt}\"";

            var interaction = new GPTInteraction
            {
                Prompt = prompt.Prompt,
                Response = generatedResponse,
                Active = true
                // CreatedAt is set by SQL
            };

            // ⬇️ remplace SaveInteractionAsync par l’UPSERT (archive + insert)
            var saved = await _gptRepository.UpsertInteractionAsync(interaction);

            await _hubContext.Clients.All.SendAsync("ReceiveGptResponse", new
            {
                prompt = saved?.Prompt ?? interaction.Prompt,
                response = saved?.Response ?? interaction.Response,
                createdAt = saved?.CreatedAt ?? DateTime.UtcNow
            });

            var groupedSuggestions = await _gptRepository.GetSuggestionsGroupedByPlaceAsync(
                typeFilter: "Swimming area", indoorFilter: false, sinceDate: DateTime.UtcNow.AddDays(-1));

            return Ok(new
            {
                prompt = saved?.Prompt ?? interaction.Prompt,
                response = saved?.Response ?? interaction.Response,
                createdAt = saved?.CreatedAt ?? DateTime.UtcNow,
                groupedSuggestions
            });
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

