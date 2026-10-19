using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Models
{
    /// <summary>
    /// A search hit. Google hits carry only <see cref="PlaceId"/> and must be resolved with place details;
    /// Nominatim hits already carry coordinates in <see cref="Location"/>.
    /// </summary>
    public record PlaceSuggestion(string Label, string Country, string? Full, string? PlaceId, GeoLocation? Location);

    public record RoadRouteAlternative
    {
        public int Index { get; init; }
        /// <summary>Decoded polyline as [lat, lon] pairs.</summary>
        public IReadOnlyList<double[]> Path { get; init; } = [];
        public double Km { get; init; }
        public int Seconds { get; init; }
        public double? FuelL { get; init; }
        public IReadOnlyList<string> Labels { get; init; } = [];
        public IReadOnlyList<FerryCrossing> Ferries { get; init; } = [];
        public int TollCount { get; init; }
    }

    public record RoadRouteRequest(GeoLocation From, GeoLocation To);
}
