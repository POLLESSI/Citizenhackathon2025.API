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
using Microsoft.AspNetCore.SignalR;
using Microsoft.SqlServer.Dac.Model;
using Volo.Abp.Domain.Entities;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
//using HubEvents = CitizenHackathon2025.Shared.StaticConfig.Constants.SuggestionHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("suggestion")]
    [Route("suggestions")]
    [Authorize]
    public class SuggestionsController : ControllerBase
    {
        private readonly ISuggestionRepository _repo;
        private readonly IHubContext<OutZenHub> _hub;

        private const string HubMethod_ReceiveSuggestionUpdate = "ReceiveSuggestionUpdate";
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

        try
        {
            // 1) Minimal normalization
            dto.OriginalPlace = dto.OriginalPlace?.Trim();
            dto.SuggestedAlternatives = dto.SuggestedAlternatives?.Trim();
            dto.Reason = dto.Reason?.Trim();
            dto.Context = dto.Context?.Trim();

            // 2) Default properties
            if (dto.DateSuggestion == default)
            dto.DateSuggestion = DateTime.UtcNow;

            // 3) Mapping DTO -> Entity (only once)
            var entity = dto.MapToSuggestion();

            // 4) Persistence
            var saved = await _repo.SaveSuggestionAsync(entity, ct);
            if (saved is null)
            {
                // Uniqueness conflict or other business logic (to be adapted)
                return Conflict($"A suggestion '{dto.OriginalPlace}' at '{dto.DateSuggestion:yyyy-MM-dd HH:mm:ss}' already exists.");
            }

            // 5) Possible broadcast via SignalR (optional)
            if (saved.EventId is int evId)
            {
                await _hub.Clients.Group($"event:{evId}")
                                  .SendAsync("NewSuggestion", saved.MapToSuggestionDTO(), ct);
            }

            // 6) 201 Created + Location
            var resultDto = saved.MapToSuggestionDTO();
            return CreatedAtAction(nameof(GetById), new { id = saved.Id }, resultDto);
        }
        catch (Exception ex)
        {
            return Problem(title: "SaveSuggestion failed", detail: ex.Message, statusCode: 500);
        }
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