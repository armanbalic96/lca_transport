using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Infrastructure.Geo
{
    /// <summary>
    /// Google Places API (New) + Routes API over REST, called server-side so the key stays out of the browser.
    /// Place search falls back to OpenStreetMap Nominatim when Google is not configured or returns nothing.
    /// </summary>
    public partial class GeoService : IGeoService
    {
        public const string GoogleClient = "google";
        public const string NominatimClient = "nominatim";

        private static readonly TimeSpan RouteCacheTtl = TimeSpan.FromHours(1);

        private readonly IHttpClientFactory _http;
        private readonly IMemoryCache _cache;
        private readonly GoogleMapsOptions _options;
        private readonly ILogger<GeoService> _logger;

        public GeoService(IHttpClientFactory http, IMemoryCache cache, IOptions<GoogleMapsOptions> options, ILogger<GeoService> logger)
        {
            _http = http;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        private bool HasGoogle => !string.IsNullOrWhiteSpace(_options.ServerApiKey);

        public async Task<IReadOnlyList<PlaceSuggestion>> Autocomplete(string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return [];
            if (HasGoogle)
            {
                try
                {
                    var google = await GoogleAutocomplete(query.Trim(), ct);
                    if (google.Count > 0) return google;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Google autocomplete failed, falling back to Nominatim");
                }
            }
            return await NominatimSearch(query.Trim(), ct);
        }

        public async Task<GeoLocation?> PlaceDetails(string placeId, CancellationToken ct)
        {
            if (!HasGoogle || string.IsNullOrWhiteSpace(placeId)) return null;

            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}");
            req.Headers.Add("X-Goog-Api-Key", _options.ServerApiKey);
            req.Headers.Add("X-Goog-FieldMask", "displayName,formattedAddress,location,addressComponents");
            using var res = await _http.CreateClient(GoogleClient).SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var p = doc.RootElement;
            if (!p.TryGetProperty("location", out var loc)) return null;

            var formatted = Str(p, "formattedAddress");
            var country = p.TryGetProperty("addressComponents", out var comps)
                ? comps.EnumerateArray()
                    .FirstOrDefault(c => c.TryGetProperty("types", out var t) && t.EnumerateArray().Any(x => x.GetString() == "country"))
                    is { ValueKind: JsonValueKind.Object } cc ? Str(cc, "longText") : null
                : null;
            var display = p.TryGetProperty("displayName", out var dn) ? Str(dn, "text") : null;

            return new GeoLocation
            {
                Label = display ?? formatted?.Split(',')[0] ?? "",
                Full = formatted ?? display,
                Country = country ?? "",
                Lat = loc.GetProperty("latitude").GetDouble(),
                Lon = loc.GetProperty("longitude").GetDouble(),
            };
        }

        public async Task<IReadOnlyList<RoadRouteAlternative>> RoadRoutes(GeoLocation from, GeoLocation to, CancellationToken ct)
        {
            if (!HasGoogle) return [];
            var key = string.Create(CultureInfo.InvariantCulture, $"road:{from.Lat:F4},{from.Lon:F4};{to.Lat:F4},{to.Lon:F4}");
            if (_cache.TryGetValue(key, out IReadOnlyList<RoadRouteAlternative>? cached) && cached is not null) return cached;

            var body = new
            {
                origin = new { location = new { latLng = new { latitude = from.Lat, longitude = from.Lon } } },
                destination = new { location = new { latLng = new { latitude = to.Lat, longitude = to.Lon } } },
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_AWARE_OPTIMAL",
                computeAlternativeRoutes = true,
                requestedReferenceRoutes = new[] { "FUEL_EFFICIENT" },
                routeModifiers = new { vehicleInfo = new { emissionType = "DIESEL" } },
                languageCode = "en-US",
                units = "METRIC",
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://routes.googleapis.com/directions/v2:computeRoutes")
            {
                Content = JsonContent.Create(body),
            };
            req.Headers.Add("X-Goog-Api-Key", _options.ServerApiKey);
            req.Headers.Add("X-Goog-FieldMask",
                "routes.distanceMeters,routes.duration,routes.routeLabels,routes.polyline.encodedPolyline," +
                "routes.travelAdvisory.fuelConsumptionMicroliters,routes.legs.steps.navigationInstruction," +
                "routes.legs.steps.distanceMeters,routes.legs.steps.staticDuration");

            using var res = await _http.CreateClient(GoogleClient).SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Routes returned {Status}", (int)res.StatusCode);
                return [];
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var routes = doc.RootElement.TryGetProperty("routes", out var r) ? r.EnumerateArray().ToList() : [];
            var result = routes
                .Select(ParseRoute)
                .OrderBy(a => a.Km)
                .Select((a, i) => a with { Index = i })
                .ToList();

            _cache.Set(key, (IReadOnlyList<RoadRouteAlternative>)result, RouteCacheTtl);
            return result;
        }

        private static RoadRouteAlternative ParseRoute(JsonElement rt)
        {
            var steps = rt.TryGetProperty("legs", out var legs)
                ? legs.EnumerateArray().SelectMany(l => l.TryGetProperty("steps", out var s) ? s.EnumerateArray() : Enumerable.Empty<JsonElement>()).ToList()
                : [];
            string Instruction(JsonElement s) =>
                s.TryGetProperty("navigationInstruction", out var ni) ? Str(ni, "instructions") ?? "" : "";

            var ferries = steps
                .Where(s => FerryRegex().IsMatch(Instruction(s)))
                .Select(s => new FerryCrossing
                {
                    Text = TakeFerryName(Instruction(s)),
                    Km = (s.TryGetProperty("distanceMeters", out var dm) ? dm.GetDouble() : 0) / 1000,
                    Minutes = (int)Math.Round(Seconds(Str(s, "staticDuration")) / 60.0),
                })
                .ToList();

            double? fuelL = rt.TryGetProperty("travelAdvisory", out var ta) && ta.TryGetProperty("fuelConsumptionMicroliters", out var fuel)
                ? double.Parse(fuel.ValueKind == JsonValueKind.String ? fuel.GetString()! : fuel.GetRawText(), CultureInfo.InvariantCulture) / 1e6
                : null;

            return new RoadRouteAlternative
            {
                Path = rt.TryGetProperty("polyline", out var pl) ? PolylineDecoder.Decode(Str(pl, "encodedPolyline")) : [],
                Km = (rt.TryGetProperty("distanceMeters", out var d) ? d.GetDouble() : 0) / 1000,
                Seconds = Seconds(Str(rt, "duration")),
                FuelL = fuelL,
                Labels = rt.TryGetProperty("routeLabels", out var lbl) ? lbl.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [],
                Ferries = ferries,
                TollCount = steps.Count(s => Instruction(s).Contains("toll road", StringComparison.OrdinalIgnoreCase)),
            };
        }

        private static string TakeFerryName(string instruction)
        {
            var first = instruction.Split('\n')[0];
            first = Regex.Replace(first, "^Take the ", "", RegexOptions.IgnoreCase);
            return Regex.Replace(first, " ferry$", "", RegexOptions.IgnoreCase).Trim();
        }

        private async Task<IReadOnlyList<PlaceSuggestion>> GoogleAutocomplete(string query, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:autocomplete")
            {
                Content = JsonContent.Create(new { input = query }),
            };
            req.Headers.Add("X-Goog-Api-Key", _options.ServerApiKey);
            using var res = await _http.CreateClient(GoogleClient).SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("suggestions", out var suggestions)) return [];
            return suggestions.EnumerateArray()
                .Where(s => s.TryGetProperty("placePrediction", out _))
                .Select(s =>
                {
                    var p = s.GetProperty("placePrediction");
                    var full = p.TryGetProperty("text", out var t) ? Str(t, "text") : null;
                    string? main = null, secondary = null;
                    if (p.TryGetProperty("structuredFormat", out var sf))
                    {
                        main = sf.TryGetProperty("mainText", out var m) ? Str(m, "text") : null;
                        secondary = sf.TryGetProperty("secondaryText", out var sec) ? Str(sec, "text") : null;
                    }
                    return new PlaceSuggestion(main ?? full ?? "", secondary ?? "", full, Str(p, "placeId"), null);
                })
                .ToList();
        }

        private async Task<IReadOnlyList<PlaceSuggestion>> NominatimSearch(string query, CancellationToken ct)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&limit=6&q={Uri.EscapeDataString(query)}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(_options.NominatimUserAgent);
                req.Headers.AcceptLanguage.ParseAdd("en");
                using var res = await _http.CreateClient(NominatimClient).SendAsync(req, ct);
                if (!res.IsSuccessStatusCode) return [];

                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                return doc.RootElement.EnumerateArray().Select(d =>
                {
                    var display = Str(d, "display_name") ?? "";
                    var label = string.Join(",", display.Split(',').Take(3)).Trim();
                    var country = d.TryGetProperty("address", out var a) ? Str(a, "country") ?? "" : "";
                    var loc = new GeoLocation
                    {
                        Label = label,
                        Full = display,
                        Country = country,
                        Lat = double.Parse(Str(d, "lat") ?? "0", CultureInfo.InvariantCulture),
                        Lon = double.Parse(Str(d, "lon") ?? "0", CultureInfo.InvariantCulture),
                    };
                    return new PlaceSuggestion(label, country, display, null, loc);
                }).ToList();
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Nominatim search failed");
                return [];
            }
        }

        private static string? Str(JsonElement e, string prop) =>
            e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static int Seconds(string? duration) =>
            int.TryParse((duration ?? "0").TrimEnd('s'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : 0;

        [GeneratedRegex(@"\bferry\b|\bferje\b", RegexOptions.IgnoreCase)]
        private static partial Regex FerryRegex();
    }
}
