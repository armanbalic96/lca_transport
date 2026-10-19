namespace TransportGwp.Domain.Models
{
    public class GeoLocation
    {
        public string Label { get; set; } = "";
        public string? Full { get; set; }
        public string? Country { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class FerryCrossing
    {
        public string Text { get; set; } = "";
        public double Km { get; set; }
        public int Minutes { get; set; }
    }

    /// <summary>Details of the Google road route chosen for a leg (display only).</summary>
    public class RouteInfo
    {
        public int Seconds { get; set; }
        public List<FerryCrossing> Ferries { get; set; } = [];
        public int TollCount { get; set; }
        public double? FuelL { get; set; }
    }

    public class TransportLeg
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public GeoLocation? From { get; set; }
        public GeoLocation? To { get; set; }
        public string Mode { get; set; } = "truck-large";
        public string Vehicle { get; set; } = "default";
        public string Fuel { get; set; } = "default";
        /// <summary>Load factor in %, clamped to 15–100 in calculations.</summary>
        public double Load { get; set; } = 61;
        /// <summary>Count an empty return trip (×2).</summary>
        public bool Ret { get; set; }
        /// <summary>User-entered distance, in <see cref="DistUnit"/>. Only used for non-road modes.</summary>
        public double? ManualDist { get; set; }
        public string DistUnit { get; set; } = "km";
        /// <summary>Road distance from the chosen Google route. Only used for road modes.</summary>
        public double? RoadKm { get; set; }
        public RouteInfo? RouteInfo { get; set; }
    }

    public class CargoItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public bool ByVolume { get; set; }
        /// <summary>Mass in tonnes (internal unit). <see cref="MassUnit"/> is the display unit.</summary>
        public double Mass { get; set; } = 1;
        public string MassUnit { get; set; } = "t";
        public string Material { get; set; } = "concrete";
        /// <summary>Volume in m³, used when <see cref="ByVolume"/> is set.</summary>
        public double Volume { get; set; } = 1;
    }

    /// <summary>
    /// The full wizard state of one transport: cargo, route (single or multi-leg) and UI progress.
    /// It is both the calculation input and what is persisted in the transport register.
    /// </summary>
    public class TransportState
    {
        public int Step { get; set; }
        public int MaxReached { get; set; }
        public string Source { get; set; } = "en15804";
        public List<CargoItem> CargoItems { get; set; } = [];

        // Single direct route
        public GeoLocation? From { get; set; }
        public GeoLocation? To { get; set; }
        public string Mode { get; set; } = "truck-large";
        public double LoadFactor { get; set; } = 61;
        public double? ManualDist { get; set; }
        public string DistUnit { get; set; } = "km";
        public double? RoadKm { get; set; }

        // Multi-leg route
        public bool Multi { get; set; }
        public List<TransportLeg> Legs { get; set; } = [];

        /// <summary>Chosen Google road alternative per map segment key.</summary>
        public Dictionary<string, int> RouteChoice { get; set; } = [];
    }
}
