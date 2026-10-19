namespace TransportGwp.Infrastructure.Geo
{
    public class GoogleMapsOptions
    {
        public const string Section = "GoogleMaps";

        /// <summary>Server key for Places API (New) and Routes API. Never sent to the browser.</summary>
        public string? ServerApiKey { get; set; }

        /// <summary>Browser key for rendering the map with the Maps JavaScript API. Restrict it by HTTP referrer.</summary>
        public string? BrowserApiKey { get; set; }

        /// <summary>User-Agent for OpenStreetMap Nominatim, required by its usage policy.</summary>
        public string NominatimUserAgent { get; set; } = "TransportGwpCalculator/1.0";
    }
}
