using CitizenHackathon2025.API.Models;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
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
        private readonly IMistralAIService _mistralAIService;
        private readonly IOpenWeatherService _weatherService;
        private readonly ILocalAiContextService _localAiContextService;
        private readonly IHubContext<AISuggestionHub> _hubContext;

        public TestAIController(
            IMistralAIService mistralAIService,
            IOpenWeatherService weatherService,
            ILocalAiContextService localAiContextService,
            IHubContext<AISuggestionHub> hubContext)
        {
            _mistralAIService = mistralAIService;
            _weatherService = weatherService;
            _localAiContextService = localAiContextService;
            _hubContext = hubContext;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("MistralAIService + LocalAiContextService injected correctly.");
        }

        [HttpPost("suggest")]
        public async Task<IActionResult> SuggestAI([FromBody] SuggestAIRequest request, CancellationToken ct)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Missing prompt.");

            var location = ExtractLocationFromPrompt(request.Prompt);

            var coordinates = await _weatherService.GetCoordinatesAsync(location, ct);

            if (coordinates is null)
            {
                return BadRequest(new
                {
                    error = "Unable to geocode location.",
                    location,
                    originalPrompt = request.Prompt
                });
            }

            var localContext = await _localAiContextService.BuildContextAsync(
                request.Prompt,
                coordinates.Value.lat,
                coordinates.Value.lon,
                ct);

            var groundedPrompt = _localAiContextService.BuildPrompt(localContext);

            var suggestion = await _mistralAIService.GenerateFromPromptAsync(groundedPrompt: groundedPrompt, ct: ct);

            await _hubContext.Clients.All.SendAsync("ReceiveAISuggestion", new
            {
                provider = "Ollama/Mistral local",
                location,
                latitude = coordinates.Value.lat,
                longitude = coordinates.Value.lon,
                originalPrompt = request.Prompt,
                suggestion
            }, ct);

            return Ok(new
            {
                provider = "Ollama/Mistral local",
                grounding = "LocalAiContextService",
                location,
                latitude = coordinates.Value.lat,
                longitude = coordinates.Value.lon,
                contextStats = new
                {
                    places = localContext.Places?.Count ?? 0,
                    events = localContext.Events?.Count ?? 0,
                    crowdCalendar = localContext.CrowdCalendar?.Count ?? 0,
                    crowdInfo = localContext.CrowdInfo?.Count ?? 0,
                    traffic = localContext.Traffic?.Count ?? 0,
                    weather = localContext.Weather?.Count ?? 0
                },
                originalPrompt = request.Prompt,
                suggestion
            });
        }

        private static string ExtractLocationFromPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "Brussels,BE";

            var normalized = prompt.Trim();

            var markers = new[]
            {
                "autour de",
                "près de",
                "proche de",
                "aux alentours de",
                "à "
            };

            foreach (var marker in markers)
            {
                var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    var value = normalized[(index + marker.Length)..]
                        .Replace("?", "")
                        .Replace("!", "")
                        .Trim();

                    value = value
                        .Replace(" en Belgique", "", StringComparison.OrdinalIgnoreCase)
                        .Replace(" Belgique", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(value))
                        return $"{value},BE";
                }
            }

            return $"{normalized},BE";
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.