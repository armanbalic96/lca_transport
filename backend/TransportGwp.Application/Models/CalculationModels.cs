namespace TransportGwp.Application.Models
{
    public record LegResult
    {
        public int Index { get; init; }
        public string Key { get; init; } = "";
        public string Mode { get; init; } = "";
        public string ModeLabel { get; init; } = "";
        public string? From { get; init; }
        public string? To { get; init; }
        public bool Ret { get; init; }
        public double Load { get; init; }
        /// <summary>Great-circle distance between the leg's end points.</summary>
        public double GreatCircleKm { get; init; }
        /// <summary>One-way distance used in the calculation.</summary>
        public double DistanceKm { get; init; }
        /// <summary>"road" (Google), "manual" (entered) or "estimate" (great-circle × routing factor).</summary>
        public string DistanceSource { get; init; } = "estimate";
        public double BaseEf { get; init; }
        public double VehicleMult { get; init; }
        public double FuelMult { get; init; }
        public double LoadAdj { get; init; }
        /// <summary>kg CO2-eq per t·km after vehicle, fuel and load adjustments.</summary>
        public double EffectiveEf { get; init; }
        public int Trips { get; init; }
        public double Tkm { get; init; }
        public double Gwp { get; init; }
        /// <summary>Share of the route total GWP, in %.</summary>
        public double Share { get; init; }
    }

    public record RouteImpact
    {
        public string Source { get; init; } = "";
        public string SourceLabel { get; init; } = "";
        public double MassTonnes { get; init; }
        public int LegCount { get; init; }
        /// <summary>Total travelled distance, including return trips.</summary>
        public double DistanceKm { get; init; }
        public double Tkm { get; init; }
        public double Gwp { get; init; }
        /// <summary>kg CO2-eq per t·km (module rate).</summary>
        public double Intensity { get; init; }
        /// <summary>kg CO2-eq per kg transported product.</summary>
        public double GwpPerKg { get; init; }
        public IReadOnlyList<LegResult> PerLeg { get; init; } = [];
    }

    public record ModeAlternative(string Mode, string ModeLabel, double Gwp, double Intensity, double DistanceKm, double DeltaPct, bool IsCurrent);

    public record ModeRecommendation(string Mode, string ModeLabel, double Gwp, double ReductionPct, string Text);

    public record CurrentMode(string? Mode, string ModeLabel, double Gwp);

    public record ModeComparison
    {
        public CurrentMode Current { get; init; } = new(null, "", 0);
        public ModeAlternative? Best { get; init; }
        public ModeAlternative? Worst { get; init; }
        /// <summary>% the best mode saves vs the worst.</summary>
        public double SavingsVsWorst { get; init; }
        /// <summary>% the best mode saves vs the current route.</summary>
        public double SavingsVsCurrent { get; init; }
        public bool IsCurrentBest { get; init; }
        public IReadOnlyList<ModeAlternative> Ranked { get; init; } = [];
        public IReadOnlyList<ModeRecommendation> Recommendations { get; init; } = [];
    }

    public record ValidationMessage(string Level, string Text);

    public record RouteValidation(bool Valid, string Level, double DistanceKm, IReadOnlyList<ValidationMessage> Messages);

    public record MaterialShare(string Name, double Tonnes, double Pct);

    public record CargoSummary(double TotalTonnes, IReadOnlyList<MaterialShare> ByMaterial, string Label, int Count);

    public record DescriptiveStats(int N, double Min, double Max, double Mean, double Median, double Std, double Sum, double Cv);

    public record ModeShare(string Label, double Gwp, double Share);

    public record RouteTotals
    {
        public double Gwp { get; init; }
        public double DistanceKm { get; init; }
        public double Tkm { get; init; }
        public double Intensity { get; init; }
        public double GwpPerKm { get; init; }
        public double GwpPerKg { get; init; }
        public int LegCount { get; init; }
        public int ModeCount { get; init; }
        public double MeanPerLeg { get; init; }
        public string? TopLegKey { get; init; }
    }

    public record RouteStatistics(
        RouteTotals Total,
        IReadOnlyList<ModeShare> ModeShares,
        DescriptiveStats GwpSpread,
        DescriptiveStats DistanceSpread,
        DescriptiveStats IntensitySpread);

    public record IndicatorResult(string Key, string Abbr, string Name, string Unit, double? Value);

    /// <summary>Everything the results step needs, computed from one <see cref="Domain.Models.TransportState"/>.</summary>
    public record ScenarioResult
    {
        public CargoSummary Cargo { get; init; } = new(0, [], "", 0);
        public RouteValidation Validation { get; init; } = new(false, "error", 0, []);
        public double GreatCircleKm { get; init; }
        public RouteImpact Impact { get; init; } = new();
        public ModeComparison Comparison { get; init; } = new();
        public RouteStatistics Statistics { get; init; } = null!;
        public IReadOnlyList<IndicatorResult> Indicators { get; init; } = [];
    }

    public record ExportBundle(string Report, string Csv, string Json);
}
