using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
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
            return Ok(interactions);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var interaction = await _gptRepository.GetByIdAsync(id);
            if (interaction == null)
                return NotFound($"Interaction with ID {id} not found.");

            return Ok(interaction);
        }
        /// <summary>
        /// Sends a query to the AI ​​and returns an intelligent response.
        /// The response is also broadcast via SignalR to all connected clients.
        /// </summary>
        /// <param name="prompt">The text sent by the user.</param>
        /// <returns>An AI-generated response (simulated here).</returns>
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string question)
        {
            var response = await _gptRepository.AskAsync(question);
            return Ok(response);
        }
        [HttpPost("ask-gpt")]
        public async Task<IActionResult> AskGpt([FromBody] GptPrompt prompt)
        {
            if (string.IsNullOrWhiteSpace((string?)(prompt?.Content)))
                return BadRequest("Prompt cannot be empty");

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

            return Ok(new
            {
                prompt = interaction.Prompt,
                response = interaction.Response
            });
        }
        [HttpGet("test-gpt")]
        public async Task<IActionResult> TestGPT([FromServices] IAIService aiService)
        {
            var result = await aiService.AskChatGptAsync("Give me 3 ideas for activities on a rainy day in Han-Sur-Lesse.");
            return Ok(result);
        }
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var success = await _gptRepository.DeactivateInteractionAsync(id);
        //    if (!success)
        //        return NotFound($"No active GPT interaction with ID {id} found.");

        //    return Ok(new { message = "Interaction deactivated successfully." });
        //}
        [HttpPost("replay/{id}")]
        public async Task<IActionResult> ReplayInteraction(int id)
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

    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.

