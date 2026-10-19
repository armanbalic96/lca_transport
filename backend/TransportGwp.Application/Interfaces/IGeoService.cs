using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Interfaces
{
    /// <summary>Place search and road routing (Google Places/Routes, with OpenStreetMap Nominatim as search fallback).</summary>
    public interface IGeoService
    {
        Task<IReadOnlyList<PlaceSuggestion>> Autocomplete(string query, CancellationToken ct);
        Task<GeoLocation?> PlaceDetails(string placeId, CancellationToken ct);

        /// <summary>All road alternatives between two points, sorted by distance. Empty when routing is unavailable.</summary>
        Task<IReadOnlyList<RoadRouteAlternative>> RoadRoutes(GeoLocation from, GeoLocation to, CancellationToken ct);
    }
}
