using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using CitizenHackathon2025.DTOs.DTOs;
using HubEvents = CitizenHackathon2025.Shared.StaticConfig.Constants.GptInteractionHubMethods;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    [Route("[controller]")]
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
            var dtos = interactions.Select(e => e.MapToGptInteractionDTO()).ToList();
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
            var response = await _gptRepository.AskAsync(question);
            return Ok(response);

            
        }

        [HttpPost("ask-gpt")]
        public async Task<IActionResult> AskGpt([FromBody] GptPrompt prompt)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace((string?)(prompt?.Content)))
                return BadRequest("Prompt cannot be empty");
            try
            {
                // 🔁 Mock response — to be replaced later
                string generatedResponse = $"[Simulated GPT] Response to: \"{prompt.Content}\"";

                // 💾 Recording in the real GptInteractions table
                var interaction = new GPTInteraction
                {
                    Prompt = (string)prompt.Content,
                    Response = generatedResponse,
                    CreatedAt = DateTime.UtcNow,
                    Active = true
                };

                await _gptRepository.SaveInteractionAsync(interaction);

                // 📡 Sending via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveGptResponse", new
                {
                    prompt = interaction.Prompt,
                    response = interaction.Response,
                    createdAt = interaction.CreatedAt
                });
                var groupedSuggestions = await _gptRepository.GetSuggestionsGroupedByPlaceAsync(typeFilter: "Swimming area", indoorFilter: false, sinceDate: DateTime.UtcNow.AddDays(-1));

                return Ok(new
                {
                    prompt = interaction.Prompt,
                    response = interaction.Response,
                    createdAt = interaction.CreatedAt,
                    groupedSuggestions
                });
            }
            catch (Exception ex)
            {

                return Problem(title: "Ask to GPT failed", detail: ex.Message, statusCode: 500);
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
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.

