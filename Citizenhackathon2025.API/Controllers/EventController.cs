using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;
        private readonly IHubContext<EventHub> _hubContext;

        public EventController(IEventRepository eventRepository, IHubContext<EventHub> hubContext)
        {
            _eventRepository = eventRepository;
            _hubContext = hubContext;
        }
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestEvent()
        {
            var events = await _eventRepository.GetLatestEventAsync(); // 👈 appel correct
            return Ok(events);
        }
        [HttpGet("upcoming-outdoor")]
        public async Task<IActionResult> GetOutdoorEvents()
        {
            var events = await _eventRepository.GetUpcomingOutdoorEventsAsync();
            return Ok(events);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");
            var @event = await _eventRepository.GetByIdAsync(id);
            if (@event == null)
                return NotFound($"No events found for the ID {id}.");
            return Ok(@event);
        }
        [HttpPost("save")]
        public async Task<IActionResult> SaveEvent([FromBody] Event @event)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var savedEvent = await _eventRepository.SaveEventAsync(@event); // 👈 correction du paramètre

            if (savedEvent == null)
                return StatusCode(500, "Error while saving");

            // ✅ Diffusion en temps réel
            await _hubContext.Clients.All.SendAsync("NewEvent", savedEvent);

            return Ok(savedEvent);
        }
        [HttpPost]
        public async Task<IActionResult> CreateEvent(EventDTO dto)
        {
            var newEvent = new Event
            {
                Name = dto.Name,
                Latitude = dto.Latitude,
                DateEvent = dto.DateEvent,
                IsOutdoor = dto.IsOutdoor
            };

            var created = await _eventRepository.CreateEventAsync(newEvent);
            return CreatedAtAction(nameof(GetOutdoorEvents), new { id = created.Id }, created);
        }
        [HttpPost("archive-expired")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ArchiveExpiredEvents()
        {
            var archived = await _eventRepository.ArchivePastEventsAsync();
            return Ok(new { ArchivedCount = archived });
        }
        [HttpPut("update")]
        public IActionResult UpdateEvent([FromBody] Event @event)
        {
            var result = _eventRepository.UpdateEvent(@event);
            return result != null ? Ok(result) : NotFound();
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.