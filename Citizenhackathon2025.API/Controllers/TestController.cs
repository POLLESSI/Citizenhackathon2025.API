using CitizenHackathon2025.Infrastructure.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly CitizenSuggestionService _service;

        public TestController(CitizenSuggestionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("suggestion")]
        public async Task<IActionResult> GetSuggestion()
        {
            var result = await _service.GetPersonalizedSuggestionsAsync("Brusssels", 1);
            return Ok(result);
        }
    }
}


















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.