using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using HubEvents = CitizenHackathon2025.Contracts.Hubs.EventHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;
        private readonly IHubContext<EventHub> _hubContext;

        private const string HubMethod_ReceiveEventUpdate = "ReceiveEventUpdate";

        public EventController(IEventRepository eventRepository, IHubContext<EventHub> hubContext)
        {
            _eventRepository = eventRepository;
            _hubContext = hubContext;
        }
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestEvent()
        {
            var entities = await _eventRepository.GetLatestEventAsync();
            var dtos = entities.Select(e => e.MapToEventDTO()).ToList();
            return Ok(dtos);
        }

        [HttpGet("upcoming-outdoor")]
        public async Task<IActionResult> GetOutdoorEvents()
        {
            var entities = await _eventRepository.GetUpcomingOutdoorEventsAsync();
            var dtos = entities.Select(e => e.MapToEventDTO()).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            if (id <= 0) return BadRequest("The provided ID is invalid.");

            var entity = await _eventRepository.GetByIdAsync(id);
            if (entity is null) return NotFound($"No events found for the ID {id}.");

            return Ok(entity.MapToEventDTO());
        }

        // GET /api/Event/by-name?name=concert
        [HttpGet("by-name")]
        public async Task<IActionResult> GetEventsByName([FromQuery] string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required.");

            name = name.Trim();

            if (name.Length < 2)
                return BadRequest("Name must contain at least 2 characters.");

            var entities = await _eventRepository.GetByNameAsync(name, ct);

            var dtos = entities
                .Select(e => e.MapToEventDTO())
                .ToList();

            return Ok(dtos);
        }

        // GET /api/Event/date-event?dateEvent=2026-06-28
        [HttpGet("date-event")]
        public async Task<IActionResult> GetEventsByDateEvent([FromQuery] DateTime dateEvent, CancellationToken ct)
        {
            if (dateEvent == default)
                return BadRequest("dateEvent is required.");

            var entities = await _eventRepository.GetByDateEventAsync(dateEvent, ct);

            var dtos = entities
                .Select(e => e.MapToEventDTO())
                .ToList();

            return Ok(dtos);
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpPost("save")]
        public async Task<IActionResult> SaveEvent([FromBody] EventDTO eventDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var dtoNorm = eventDto.MapToEventWithDateEvent();
                var entity = dtoNorm.MapToEvent(); // DTO -> Entity

                var saved = await _eventRepository.SaveEventAsync(entity);
                if (saved is null)
                {
                    return Conflict($"An event named '{eventDto.Name}' at '{dtoNorm.DateEvent:yyyy-MM-dd HH:mm:ss}' already exists.");
                }

                var savedDto = saved.MapToEventDTO();

                await _hubContext.Clients.All.SendAsync(HubMethod_ReceiveEventUpdate, savedDto);

                return Ok(savedDto);
            }
            catch (Exception ex)
            {
                return Problem(title: "SaveEvent failed", detail: ex.Message, statusCode: 500);
            }
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateEvent([FromBody] EventDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newEvent = new Event
            {
                Name = dto.Name,
                PlaceId = dto.PlaceId,
                Latitude = (decimal)Math.Round(dto.Latitude, 2),   
                Longitude = (decimal)Math.Round(dto.Longitude, 3), 
                DateEvent = new DateTime(dto.DateEvent.Ticks - (dto.DateEvent.Ticks % TimeSpan.TicksPerSecond), dto.DateEvent.Kind),
                ExpectedCrowd = dto.ExpectedCrowd,                  
                IsOutdoor = dto.IsOutdoor,
                Active = true
            };

            var created = await _eventRepository.CreateEventAsync(newEvent);
            var createdDto = created.MapToEventDTO();
            return CreatedAtAction(nameof(GetEventById), new { id = created.Id }, createdDto);
        }
        [HttpPost("archive-expired")]
        [Authorize(Policy = "AdminOrModo")]
        public async Task<IActionResult> ArchiveExpiredEvents()
        {
            var archived = await _eventRepository.ArchivePastEventsAsync();
            return Ok(new { ArchivedCount = archived });
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpPut("update")]
        public IActionResult UpdateEvent([FromBody] Event @event)
        {
            var result = _eventRepository.UpdateEvent(@event);
            return result != null ? Ok(result) : NotFound();
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.