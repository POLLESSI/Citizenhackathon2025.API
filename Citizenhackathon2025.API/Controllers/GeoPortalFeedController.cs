using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs.GeoPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/geoportal-feed")]
    public sealed class GeoPortalFeedController : ControllerBase
    {
        private readonly IGeoPortalFeedService _geoPortalFeedService;

        public GeoPortalFeedController(IGeoPortalFeedService geoPortalFeedService)
        {
            _geoPortalFeedService = geoPortalFeedService;
        }


        /// <summary>
        /// Returns the current snapshot.
        /// Uses the cache when available.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(GeoPortalFeedSnapshotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GeoPortalFeedSnapshotDto), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<GeoPortalFeedSnapshotDto>>
            GetAsync(CancellationToken cancellationToken)
        {
            var snapshot = await _geoPortalFeedService.GetAsync(cancellationToken);

            /*
             * Even though the sources have dried up,
             * if OutZen has a stale cache,
             * we return 200 with IsStale=true.
             */
            if (snapshot.Items.Count > 0)
            {
                return Ok(snapshot);
            }

            if (!snapshot.IsSuccess)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, snapshot);
            }

            return Ok(snapshot);
        }


        /// <summary>
        /// Force a new read of the remote feeds.
        ///
        /// Important protection:
        /// do not allow any visitor
        /// to bypass IMemoryCache.
        /// </summary>
        [HttpPost("refresh")]
        [Authorize(Policy = "AdminOrModo")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(GeoPortalFeedSnapshotDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<GeoPortalFeedSnapshotDto>>RefreshAsync(CancellationToken cancellationToken)
        {
            var snapshot = await _geoPortalFeedService.RefreshAsync(cancellationToken);

            if (snapshot.Items.Count > 0)
            {
                return Ok(snapshot);
            }

            if (!snapshot.IsSuccess)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, snapshot);
            }
            return Ok(snapshot);
        }
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.