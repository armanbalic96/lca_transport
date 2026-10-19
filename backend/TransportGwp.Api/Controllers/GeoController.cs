using Microsoft.AspNetCore.Mvc;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Api.Controllers
{
    /// <summary>Server-side proxy for place search and road routing, so map provider keys stay on the server.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GeoController : ControllerBase
    {
        private readonly IGeoService _geoService;

        public GeoController(IGeoService geoService)
            => _geoService = geoService;

        [HttpGet("autocomplete")]
        public async Task<ActionResult<IReadOnlyList<PlaceSuggestion>>> Autocomplete([FromQuery] string q, CancellationToken ct)
            => Ok(await _geoService.Autocomplete(q, ct));

        [HttpGet("places/{placeId}")]
        public async Task<ActionResult<GeoLocation>> PlaceDetails(string placeId, CancellationToken ct)
        {
            var place = await _geoService.PlaceDetails(placeId, ct);
            return place is null ? NotFound() : Ok(place);
        }

        [HttpPost("road-routes")]
        public async Task<ActionResult<IReadOnlyList<RoadRouteAlternative>>> RoadRoutes([FromBody] RoadRouteRequest request, CancellationToken ct)
            => Ok(await _geoService.RoadRoutes(request.From, request.To, ct));
    }
}
