using CitizenHackathon2025.Application.CQRS.Commands;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Suggestions.Commands;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Domain.Entities;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using HubEvents = CitizenHackathon2025.Contracts.Hubs.SuggestionHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuggestionsController : ControllerBase
    {
        private readonly ISuggestionRepository _repo;
        private readonly IHubContext<SuggestionHub> _hub;

        private const string HubMethod_ReceiveSuggestionUpdate = "ReceiveSuggestionUpdate";
        public SuggestionsController(ISuggestionRepository repo, IHubContext<SuggestionHub> hub)
        {
            _repo = repo;
            _hub = hub;
        }
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] int? userId, [FromQuery] bool all = false, CancellationToken ct = default)
        {
            if (userId.HasValue)
                return Ok(await _repo.GetSuggestionsByUserAsync(userId.Value));

            if (all)
            {
                var entities = await _repo.GetAllSuggestionsAsync();
                var dtos = entities.Select(s => s.MapToSuggestionDTO()).ToList();
                return Ok(dtos);
            }
            return Ok(await _repo.GetLatestSuggestionAsync());
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromQuery] int? userId, CancellationToken ct)
        {
            if (userId.HasValue)
                return Ok(await _repo.GetSuggestionsByUserAsync(userId.Value));

            return Ok(await _repo.GetLatestSuggestionAsync());
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SuggestionDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity is null) return NotFound();
            return Ok(entity.MapToSuggestionDTO());
        }

        [HttpPost]
        [AllowAnonymous] 
        [ProducesResponseType(typeof(SuggestionDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] SuggestionDTO dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // … normalization / mapping / save …
            var saved = await _repo.SaveSuggestionAsync(dto.MapToSuggestion(), ct);
            if (saved is null) return Conflict("Already exists.");

            var resultDto = saved.MapToSuggestionDTO();

            // ✅ broadcast with shared constants
            await _hub.Clients.All
                .SendAsync(HubEvents.ToClient.ReceiveSuggestion, resultDto, ct);

            // ping “refresh” optional
            await _hub.Clients.All
                .SendAsync(HubEvents.ToClient.NewSuggestion, ct);

            return CreatedAtAction(nameof(GetById), new { id = saved.Id }, resultDto);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] SuggestionDTO dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var entity = new Suggestion
            {
                Id = id,
                User_Id = dto.UserId,
                DateSuggestion = dto.DateSuggestion,
                OriginalPlace = dto.OriginalPlace,
                SuggestedAlternatives = dto.SuggestedAlternatives,
                Reason = dto.Reason
            };

            var updated = _repo.UpdateSuggestion(entity);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _repo.SoftDeleteSuggestionAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}




































































    























// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.