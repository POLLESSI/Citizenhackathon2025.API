using Citizenhackathon2025.API.Hubs;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TestAIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IHubContext<AISuggestionHub> _hubContext;

        public TestAIController(IAIService aiService, IHubContext<AISuggestionHub> hubContext)
        {
            _aiService = aiService;
            _hubContext = hubContext;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("AIService is injected correctly.");
        }
        [HttpPost("suggest")]
        public async Task<IActionResult> SuggestAI([FromBody] SuggestAIRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt manquant.");

            var suggestion = await _aiService.SuggestAlternativeWithWeatherAsync(request.Prompt);

            // Envoie la suggestion via SignalR à tous les clients
            await _hubContext.Clients.All.SendAsync("ReceiveAISuggestion", new
            {
                location = request.Prompt,
                suggestion
            });

            return Ok(new { location = request.Prompt, suggestion });
        }
    }
}
