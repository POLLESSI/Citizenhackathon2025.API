using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Application.CQRS.Commands;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Suggestions.Commands;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SqlServer.Dac.Model;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuggestionsController : ControllerBase
    {
        private readonly ISuggestionRepository _repo;
        private readonly IHubContext<OutZenHub> _hub;

        public SuggestionsController(ISuggestionRepository repo, IHubContext<OutZenHub> hub)
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
                return Ok(await _repo.GetAllSuggestionsAsync());

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
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var s = await _repo.GetByIdAsync(id);
            return s is { Active: true } ? Ok(s) : NotFound();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] SuggestionDTO dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Ownership: User_Id from claims if needed
            var entity = new Suggestion
            {
                User_Id = dto.UserId,
                DateSuggestion = dto.DateSuggestion == default ? DateTime.UtcNow : dto.DateSuggestion,
                OriginalPlace = dto.OriginalPlace,
                SuggestedAlternatives = dto.SuggestedAlternatives,
                Reason = dto.Reason,
                LocationName = dto.Context, // or a dedicated field if you want
                EventId = dto.EventId
            };

            var saved = await _repo.SaveSuggestionAsync(entity);

            // Send to EventId group if present
            if (saved?.EventId is int evId)
                await _hub.Clients.Group($"event:{evId}").SendAsync("NewSuggestion", saved, ct);

            return CreatedAtAction(nameof(GetById), new { id = saved!.Id }, saved);
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