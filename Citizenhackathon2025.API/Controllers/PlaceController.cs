using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Numerics;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaceController : ControllerBase
    {
        private readonly IPlaceRepository _placeRepository;
        private readonly IHubContext<PlaceHub> _hubContext;

        public PlaceController(IPlaceRepository placeRepository, IHubContext<PlaceHub> hubContext)
        {
            _placeRepository = placeRepository;
            _hubContext = hubContext;
        }
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPlace()
        {
            var places = await _placeRepository.GetLatestPlaceAsync(); // 👈 correct call
            return Ok(places);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPlaceById(int id)
        {
            var place = await _placeRepository.GetPlaceByIdAsync(id); // direct call to the repo

            if (place == null)
                return NotFound($"Place with ID {id} not found.");

            return Ok(place);
        }
        [HttpPost]
        public async Task<IActionResult> SavePlace([FromBody] PlaceDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var place = new Place
            {
                Name = dto.Name,
                Type = dto.Type,
                Indoor = dto.Indoor,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Capacity = dto.Capacity,
                Tag = dto.Tag,
                //Active = true 
            };

            var savedPlace = await _placeRepository.SavePlaceAsync(place); // 👈 parameter correction

            if (savedPlace == null)
                return StatusCode(500, "Registration Error");

            // ✅ Real-time broadcasting
            await _hubContext.Clients.All.SendAsync("NewPlace", savedPlace);

            return Ok(savedPlace);
        }
        [HttpPut("update")]
        public IActionResult UpdatePlace([FromBody] Place place)
        {
            var result = _placeRepository.UpdatePlace(place);
            return result != null ? Ok(result) : NotFound();
        }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.