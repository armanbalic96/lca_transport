using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Interfaces
{
    /// <summary>Pure transport-impact calculations (EN 15804+A2, GWP-total). No I/O.</summary>
    public interface ITransportCalculator
    {
        double ItemMassTonnes(CargoItem item);
        CargoSummary SummarizeCargo(IReadOnlyList<CargoItem> items);
        RouteValidation ValidateRoute(GeoLocation? from, GeoLocation? to);
        LegResult CalculateLeg(TransportLeg leg, string source, double massTonnes, int index = 0);
        RouteImpact CalculateRouteImpact(IReadOnlyList<TransportLeg> legs, string source, double massTonnes);
        ModeComparison CompareModes(IReadOnlyList<TransportLeg> legs, string source, double massTonnes, double recommendThresholdPct = 10);
        RouteStatistics BuildStatistics(RouteImpact impact);
        IReadOnlyList<IndicatorResult> BuildIndicators(RouteImpact impact, IReadOnlyList<TransportLeg> legs, string source);

        /// <summary>The legs that make up the route: the multi-leg list, or one leg built from the direct route.</summary>
        IReadOnlyList<TransportLeg> RouteLegs(TransportState state);

        /// <summary>Runs every calculation for a wizard state.</summary>
        ScenarioResult Calculate(TransportState state);
    }
}
