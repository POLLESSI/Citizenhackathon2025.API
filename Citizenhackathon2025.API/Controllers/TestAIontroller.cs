using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Citizenhackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Route("api/testai")]
    public class TestAIontroller : ControllerBase
    {
        private readonly IAIService _ai;

        public TestAIontroller(IAIService ai)
        {
            _ai = ai;
        }
        [HttpGet]
        public IActionResult Ping()
        {
            return Ok("AIService is injected correctly.");
        }
    }
}
