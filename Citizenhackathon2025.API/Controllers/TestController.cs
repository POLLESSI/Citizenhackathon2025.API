using CitizenHackathon2025.Infrastructure.UseCases;
using CitizenHackathon2025.Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly CitizenSuggestionService _service;
        private readonly IPasswordHasher _hasher;

        public TestController(CitizenSuggestionService service, IPasswordHasher hasher)
        {
            _service = service;
            _hasher = hasher;
        }

        [HttpGet("suggestion")]
        public async Task<IActionResult> GetSuggestion()
        {
            var result = await _service.GetPersonalizedSuggestionsAsync("Brusssels", 1);
            return Ok(result);
        }
        [HttpGet("test-hash")]
        public IActionResult GetHash()
        {
            var hash = _hasher.HashPassword("Test1234=", Guid.NewGuid().ToString());
            return Ok(Convert.ToHexString(hash));
        }
    }
}
