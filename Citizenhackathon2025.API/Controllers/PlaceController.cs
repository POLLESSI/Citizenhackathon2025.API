using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using HubEvents = CitizenHackathon2025.Shared.StaticConfig.Constants.PlaceHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [ApiController]
    [Route("api/[controller]")] // => /api/Place
    public class PlaceController : ControllerBase
    {
        private readonly IPlaceRepository _placeRepository;
        private readonly IHubContext<PlaceHub> _hubContext;

        private const string HubMethod_ReceivePlaceUpdate = "ReceivePlaceUpdate";

        public PlaceController(IPlaceRepository placeRepository, IHubContext<PlaceHub> hubContext)
        {
            _placeRepository = placeRepository;
            _hubContext = hubContext;
        }

        // A. GET /api/Place/latest
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPlace()
        {
            var places = await _placeRepository.GetLatestPlaceAsync(); // 👈 correct call
            var dtos = places.Select(p => p.MapToPlaceDTO()).ToList();
            return Ok(dtos);
        }

        // B. GET /api/Place/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPlaceByIdAsync(int id, CancellationToken ct)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var place = await _placeRepository.GetPlaceByIdAsync(id);
            if (place is null)
                return NotFound($"Place with ID {id} not found.");

            return Ok(place.MapToPlaceDTO());
        }
        // C. POST /api/Place  (create)
        [HttpPost("save")]
        public async Task<IActionResult> SavePlace([FromBody] PlaceDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var dtoNorm = dto.MapToPlaceWithLatitude();
                var place = dtoNorm.MapToPlace(); // DTO -> Entity

                var savedPlace = await _placeRepository.SavePlaceAsync(place); // 👈 parameter correction

                if (savedPlace == null)
                    return Conflict($"An place named '{dto.Name}' at '{dtoNorm.Latitude}' already exists.");

                var savedDto = savedPlace.MapToPlaceDTO();

                // ✅ Real-time broadcasting
                await _hubContext.Clients.All.SendAsync(HubMethod_ReceivePlaceUpdate, savedPlace);

                return Ok(savedPlace);
            }
            catch (Exception ex)
            {

                return Problem(title: "SavePlace failed", detail: ex.Message, statusCode: 500);
            }
        }

        // E. PUT /api/Place/update
        [HttpPut("update")]
        [Consumes("application/json")]
        public async Task<IActionResult> Update([FromBody] Place dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.Id <= 0) return BadRequest("Id must be > 0.");

            var updated = await _placeRepository.UpdateAsync(dto);
            if (updated is null) return NotFound($"Place #{dto.Id} not found or inactive.");

            return Ok(updated);
        }

        // Variante REST: PUT /api/Place/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateById(int id, [FromBody] Place dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id <= 0) return BadRequest("Invalid id.");
            dto.Id = id;

            var updated = await _placeRepository.UpdateAsync(dto);
            if (updated is null) return NotFound($"Place #{id} not found or inactive.");

            return Ok(updated);
        }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.