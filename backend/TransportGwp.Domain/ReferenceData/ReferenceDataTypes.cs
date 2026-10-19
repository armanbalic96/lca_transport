namespace TransportGwp.Domain.ReferenceData
{
    /// <summary>Emission factor for one transport mode, in kg CO2-eq per tonne-km.</summary>
    public record ModeFactor(string Mode, double Ef, string Label, string Sub);

    public record EmissionFactorSource(string Id, string Label, string Sub, IReadOnlyDictionary<string, ModeFactor> Factors);

    /// <summary>Vehicle sub-class for a mode. <c>Mult</c> adjusts the base emission factor.</summary>
    public record VehicleOption(string Id, string Label, double Mult);

    /// <summary>Fuel / energy source. <c>Kind</c> tells which mode family it applies to ("any", "combustion", "marine", "aviation", "electric").</summary>
    public record FuelOption(string Id, string Label, double Mult, string Kind);

    /// <summary>Material with density in kg/m³, used for volume → mass conversion.</summary>
    public record Material(string Id, string Name, double Density);

    public record LegSeed(string Mode, string Vehicle, string Fuel, double Load, bool Ret);

    public record LegTemplate(string Id, string Label, string Sub, IReadOnlyList<LegSeed> Legs);

    /// <summary>EN 15804+A2 core environmental impact indicator.</summary>
    public record IndicatorDefinition(string Key, string Abbr, string Name, string Unit);

    public record MassUnit(string Id, string Short, double PerTonne);
}
