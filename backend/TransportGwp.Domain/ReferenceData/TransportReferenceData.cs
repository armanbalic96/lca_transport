namespace TransportGwp.Domain.ReferenceData
{
    /// <summary>
    /// Static reference data for the transport calculator.
    /// Values are illustrative defaults aligned with publicly available LCA databases.
    /// Users can override vehicle, fuel, load factor and return trip per leg.
    /// </summary>
    public static class TransportReferenceData
    {
        public const string DefaultSource = "en15804";
        public const string DefaultMode = "truck-large";

        public static readonly IReadOnlyList<string> TransportModes =
        [
            "truck-small", "truck-large", "truck-electric",
            "rail",
            "sea-container", "ferry", "air",
        ];

        public static readonly IReadOnlyDictionary<string, EmissionFactorSource> EmissionFactorSources =
            new Dictionary<string, EmissionFactorSource>
            {
                ["en15804"] = new("en15804", "EN 15804+A2", "Generic EPD values, EU", new Dictionary<string, ModeFactor>
                {
                    ["truck-small"] = new("truck-small", 0.230, "Truck <16t", "EU avg."),
                    ["truck-large"] = new("truck-large", 0.080, "Truck >16t", "EU avg."),
                    ["truck-electric"] = new("truck-electric", 0.040, "Electric truck", "EU grid mix"),
                    ["rail"] = new("rail", 0.025, "Rail freight", "EU avg."),
                    ["sea-container"] = new("sea-container", 0.012, "Sea container", "EU avg."),
                    ["ferry"] = new("ferry", 0.050, "Ferry (RoRo)", "EU avg. short sea"),
                    ["air"] = new("air", 0.600, "Air freight", "EU avg."),
                }),
            };

        /// <summary>Default load factor recommendations in % (EN 15804 informative).</summary>
        public static readonly IReadOnlyDictionary<string, double> DefaultLoad = new Dictionary<string, double>
        {
            ["truck-small"] = 50,
            ["truck-large"] = 61,
            ["truck-electric"] = 61,
            ["van"] = 50,
            ["rail"] = 65,
            ["sea-container"] = 70,
            ["sea-bulk"] = 75,
            ["inland"] = 55,
            ["ferry"] = 60,
            ["air"] = 65,
        };

        public static readonly IReadOnlyDictionary<string, IReadOnlyList<VehicleOption>> VehicleOptions =
            new Dictionary<string, IReadOnlyList<VehicleOption>>
            {
                ["truck-small"] = [new("default", "Default", 1.00), new("euro5", "EURO 5", 1.05), new("euro6", "EURO 6", 0.95)],
                ["truck-large"] = [new("default", "Default", 1.00), new("euro4", "EURO 4", 1.10), new("euro5", "EURO 5", 1.02), new("euro6", "EURO 6", 0.93)],
                ["truck-electric"] = [new("default", "Default BEV", 1.00), new("bev-rigid", "BEV rigid", 1.05), new("bev-artic", "BEV artic.", 0.95)],
                ["van"] = [new("default", "Default", 1.00), new("euro5", "EURO 5", 1.05), new("euro6", "EURO 6", 0.95)],
                ["rail"] = [new("default", "Mixed", 1.00), new("electric", "Electric", 0.55), new("diesel", "Diesel", 1.45)],
                ["sea-container"] =
                [
                    new("default", "Default", 1.00), new("small", "<3000 TEU", 1.45), new("medium", "3-8000 TEU", 1.10),
                    new("large", ">8000 TEU", 0.85), new("slow", "Slow steaming", 0.78),
                ],
                ["sea-bulk"] = [new("default", "Default", 1.00), new("handysize", "Handysize", 1.20), new("panamax", "Panamax", 1.00), new("capesize", "Capesize", 0.85)],
                ["inland"] = [new("default", "Default", 1.00), new("barge", "Self-prop. barge", 1.00), new("pusher", "Pusher convoy", 0.85)],
                ["ferry"] =
                [
                    new("default", "Default", 1.00), new("roro-large", "RoRo >2000 lane-m", 0.88),
                    new("roro-small", "RoRo <2000 lane-m", 1.15), new("ropax", "RoPax (pax + freight)", 1.25),
                ],
                ["air"] = [new("default", "Default", 1.00), new("belly", "Belly (pax)", 1.00), new("freighter", "Dedicated", 1.15), new("longhaul", "Long-haul", 0.90)],
            };

        public static readonly IReadOnlyList<FuelOption> FuelOptions =
        [
            new("default", "Default", 1.00, "any"),
            new("diesel", "Diesel", 1.00, "combustion"),
            new("biodiesel-b20", "Biodiesel B20", 0.85, "combustion"),
            new("hvo100", "HVO100", 0.20, "combustion"),
            new("lng", "LNG", 0.78, "combustion"),
            new("marine-gasoil", "Marine gas oil", 1.05, "marine"),
            new("hfo", "Heavy fuel oil", 1.10, "marine"),
            new("lng-marine", "LNG (marine)", 0.80, "marine"),
            new("jet-a1", "Jet A-1", 1.00, "aviation"),
            new("saf-30", "SAF 30% blend", 0.75, "aviation"),
            new("eu-grid", "EU grid mix", 1.00, "electric"),
            new("no-grid", "Norwegian grid", 0.10, "electric"),
            new("renewable", "Renewable PPA", 0.05, "electric"),
        ];

        public static readonly IReadOnlyDictionary<string, string> ModeFuelKind = new Dictionary<string, string>
        {
            ["truck-small"] = "combustion",
            ["truck-large"] = "combustion",
            ["truck-electric"] = "electric",
            ["van"] = "combustion",
            ["rail"] = "combustion",
            ["sea-container"] = "marine",
            ["sea-bulk"] = "marine",
            ["inland"] = "combustion",
            ["ferry"] = "marine",
            ["air"] = "aviation",
        };

        public static readonly IReadOnlyList<Material> Materials =
        [
            new("concrete", "Concrete", 2400),
            new("steel", "Steel", 7850),
            new("timber", "Timber", 500),
            new("glass", "Glass", 2500),
            new("brick", "Brick", 1800),
            new("gypsum", "Gypsum", 800),
            new("insulation", "Insulation", 30),
            new("aluminium", "Aluminium", 2700),
        ];

        public static readonly IReadOnlyList<MassUnit> MassUnits =
        [
            new("t", "t", 1),
            new("kg", "kg", 1000),
            new("Mg", "Mg", 1),
            new("lb", "lb", 2204.62),
        ];

        public static readonly IReadOnlyList<LegTemplate> LegTemplates =
        [
            new("direct-truck", "Direct truck", "1 leg · road freight",
                [new("truck-large", "default", "default", 61, false)]),
            new("eu-road", "Standard EU road freight", "1 leg · EURO 6, diesel, 61% load",
                [new("truck-large", "euro6", "diesel", 61, false)]),
            new("sea-and-truck", "Sea container + truck", "3 legs · last-mile + port-to-port + pre-haul",
            [
                new("truck-large", "default", "diesel", 61, false),
                new("sea-container", "large", "marine-gasoil", 70, false),
                new("truck-large", "default", "diesel", 61, false),
            ]),
            new("rail-and-truck", "Rail + truck (intermodal)", "3 legs · low-emission inland route",
            [
                new("truck-large", "default", "diesel", 61, false),
                new("rail", "electric", "eu-grid", 65, false),
                new("truck-large", "default", "diesel", 61, false),
            ]),
            new("nordic-electric", "Nordic electric road", "1 leg · electric truck on Norwegian grid",
                [new("truck-electric", "default", "no-grid", 61, false)]),
            new("air-express", "Air express", "1 leg · long-haul air freight",
                [new("air", "longhaul", "jet-a1", 65, false)]),
        ];

        public static readonly IReadOnlyList<IndicatorDefinition> En15804CoreIndicators =
        [
            new("gwp-total", "GWP-total", "Climate change – total", "kg CO₂ eq"),
            new("gwp-fossil", "GWP-fossil", "Climate change – fossil", "kg CO₂ eq"),
            new("gwp-biogenic", "GWP-biogenic", "Climate change – biogenic", "kg CO₂ eq"),
            new("gwp-luluc", "GWP-luluc", "Climate change – land use and land use change", "kg CO₂ eq"),
            new("odp", "ODP", "Ozone depletion", "kg CFC-11 eq"),
            new("ap", "AP", "Acidification", "mol H⁺ eq"),
            new("ep-fw", "EP-freshwater", "Eutrophication – aquatic freshwater", "kg P eq"),
            new("ep-marine", "EP-marine", "Eutrophication – aquatic marine", "kg N eq"),
            new("ep-terr", "EP-terrestrial", "Eutrophication – terrestrial", "mol N eq"),
            new("pocp", "POCP", "Photochemical ozone formation", "kg NMVOC eq"),
            new("adp-mm", "ADP-minerals&metals", "Depletion of abiotic resources – minerals and metals", "kg Sb eq"),
            new("adp-fossil", "ADP-fossil", "Depletion of abiotic resources – fossil fuels", "MJ, net calorific value"),
            new("wdp", "WDP", "Water use", "m³ world eq. deprived"),
        ];

        /// <summary>
        /// Per-tonne-km characterisation factors for the non-GWP indicators, by source → mode → indicator key.
        /// Empty today: only GWP-total has factor data. Rows without factors are reported as "No factor".
        /// </summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>> IndicatorFactors =
            new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>>();

        public static bool IsRoadMode(string? mode) =>
            mode is not null && (mode.StartsWith("truck", StringComparison.Ordinal) || mode == "van");

        /// <summary>Ratio of real travelled distance to great-circle distance, per mode.</summary>
        public static double RoutingFactor(string mode) => mode switch
        {
            _ when IsRoadMode(mode) => 1.25,
            "rail" => 1.18,
            "ferry" => 1.10,
            "inland" => 1.30,
            "sea-container" or "sea-bulk" => 1.20,
            "air" => 1.05,
            _ => 1.2,
        };

        public static double DefaultLoadFor(string mode) => DefaultLoad.TryGetValue(mode, out var load) ? load : 60;
    }
}
