using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Hubs.Hubs;
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
            var places = await _placeRepository.GetLatestPlaceAsync(); // 👈 appel correct
            return Ok(places);
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

            var savedPlace = await _placeRepository.SavePlaceAsync(place); // 👈 correction du paramètre

            if (savedPlace == null)
                return StatusCode(500, "Registration Error");

            // ✅ Diffusion en temps réel
            await _hubContext.Clients.All.SendAsync("NewPlace", savedPlace);

            return Ok(savedPlace);
        }
    }
}
