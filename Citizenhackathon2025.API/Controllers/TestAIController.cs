using CitizenHackathon2025.API.Models;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
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
                return BadRequest("Missing prompt.");

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


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.