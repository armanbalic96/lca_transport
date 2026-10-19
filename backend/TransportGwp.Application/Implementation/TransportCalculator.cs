using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;
using TransportGwp.Domain.ReferenceData;
using static TransportGwp.Application.Implementation.NumberFormat;
using Ref = TransportGwp.Domain.ReferenceData.TransportReferenceData;

namespace TransportGwp.Application.Implementation
{
    /// <summary>
    /// Transport GWP calculation:
    /// GWP = mass [t] × distance [km] × EF [kg CO2-eq/t·km] × vehicle × fuel × load adjustment × trips.
    /// </summary>
    public class TransportCalculator : ITransportCalculator
    {
        private const double MilesToKm = 1.609344;
        private const double MinRouteKm = 1;
        private const double MaxRouteKm = 20500; // ~half the earth's circumference

        public double ItemMassTonnes(CargoItem item)
        {
            if (!item.ByVolume) return item.Mass;
            var material = Ref.Materials.FirstOrDefault(m => m.Id == item.Material) ?? Ref.Materials[0];
            return item.Volume * material.Density / 1000;
        }

        public CargoSummary SummarizeCargo(IReadOnlyList<CargoItem> items)
        {
            var total = items.Sum(ItemMassTonnes);
            var byMaterial = items
                .GroupBy(i => Ref.Materials.FirstOrDefault(m => m.Id == i.Material)?.Name ?? i.Material)
                .Select(g =>
                {
                    var t = g.Sum(ItemMassTonnes);
                    return new MaterialShare(g.Key, t, total > 0 ? t / total * 100 : 0);
                })
                .OrderByDescending(m => m.Tonnes)
                .ToList();
            var label = byMaterial.Count == 1
                ? byMaterial[0].Name
                : $"{byMaterial.Count} materials ({string.Join(", ", byMaterial.Take(3).Select(m => m.Name))}{(byMaterial.Count > 3 ? "…" : "")})";
            return new CargoSummary(total, byMaterial, label, items.Count);
        }

        public RouteValidation ValidateRoute(GeoLocation? from, GeoLocation? to)
        {
            var messages = new List<ValidationMessage>();
            if (!HasPoint(from)) messages.Add(new("error", "Origin (from) is not set or has no coordinates."));
            if (!HasPoint(to)) messages.Add(new("error", "Destination (to) is not set or has no coordinates."));
            if (messages.Count > 0) return new RouteValidation(false, "error", 0, messages);

            if (Math.Abs(from!.Lat) > 90 || Math.Abs(to!.Lat) > 90 || Math.Abs(from.Lon) > 180 || Math.Abs(to.Lon) > 180)
                return new RouteValidation(false, "error", 0, [new("error", "Coordinates are out of range.")]);

            var dist = GeoMath.Haversine(from, to);
            if (dist < MinRouteKm)
                messages.Add(new("warning", $"Origin and destination are almost identical ({Fmt(dist)} km). Transport impact will be negligible."));
            if (dist > MaxRouteKm)
                messages.Add(new("warning", $"Distance of {Fmt(dist, 0)} km exceeds a plausible single transport leg — check the picked locations."));

            var level = messages.Count > 0 ? "warning" : "ok";
            if (level == "ok") messages.Add(new("ok", $"Route valid · {Fmt(dist, 0)} km great-circle."));
            return new RouteValidation(true, level, Math.Round(dist, 1), messages);

            static bool HasPoint(GeoLocation? p) => p is not null && double.IsFinite(p.Lat) && double.IsFinite(p.Lon);
        }

        /// <summary>Load adjustment: √(100 / load), with load clamped to 15–100% and the result to 1.0–2.2.</summary>
        public static double LoadAdjustment(double load)
        {
            var lf = Math.Clamp(load, 15, 100);
            return Math.Clamp(Math.Sqrt(100 / lf), 1, 2.2);
        }

        /// <summary>Entered distance in km, or null when none is set or the mode is routed by road.</summary>
        public static double? ManualKm(TransportLeg leg)
        {
            if (leg.ManualDist is not > 0 || Ref.IsRoadMode(leg.Mode)) return null;
            return leg.DistUnit == "mi" ? leg.ManualDist.Value * MilesToKm : leg.ManualDist.Value;
        }

        public LegResult CalculateLeg(TransportLeg leg, string source, double massTonnes, int index = 0)
        {
            var factors = Factors(source);
            factors.TryGetValue(leg.Mode, out var factor);
            var baseEf = factor?.Ef ?? 0;
            var vehicleMult = Ref.VehicleOptions.TryGetValue(leg.Mode, out var vehicles)
                ? vehicles.FirstOrDefault(v => v.Id == leg.Vehicle)?.Mult ?? 1
                : 1;
            var fuelMult = Ref.FuelOptions.FirstOrDefault(f => f.Id == leg.Fuel)?.Mult ?? 1;
            var loadAdj = LoadAdjustment(leg.Load);
            var greatCircle = GeoMath.Haversine(leg.From, leg.To);

            double distance;
            string distanceSource;
            if (ManualKm(leg) is { } manual)
            {
                distance = manual;
                distanceSource = "manual";
            }
            else if (leg.RoadKm is > 0 && Ref.IsRoadMode(leg.Mode))
            {
                distance = leg.RoadKm.Value;
                distanceSource = "road";
            }
            else
            {
                distance = greatCircle * Ref.RoutingFactor(leg.Mode);
                distanceSource = "estimate";
            }

            var trips = leg.Ret ? 2 : 1;
            var effectiveEf = baseEf * vehicleMult * fuelMult * loadAdj;
            return new LegResult
            {
                Index = index,
                Key = $"L{index + 1}",
                Mode = leg.Mode,
                ModeLabel = factor?.Label ?? leg.Mode,
                From = leg.From?.Label,
                To = leg.To?.Label,
                Ret = leg.Ret,
                Load = leg.Load,
                GreatCircleKm = greatCircle,
                DistanceKm = distance,
                DistanceSource = distanceSource,
                BaseEf = baseEf,
                VehicleMult = vehicleMult,
                FuelMult = fuelMult,
                LoadAdj = loadAdj,
                EffectiveEf = effectiveEf,
                Trips = trips,
                Tkm = massTonnes * distance * trips,
                Gwp = massTonnes * distance * effectiveEf * trips,
            };
        }

        public RouteImpact CalculateRouteImpact(IReadOnlyList<TransportLeg> legs, string source, double massTonnes)
        {
            var perLeg = legs.Select((l, i) => CalculateLeg(l, source, massTonnes, i)).ToList();
            var gwp = perLeg.Sum(l => l.Gwp);
            var tkm = perLeg.Sum(l => l.Tkm);
            perLeg = perLeg.Select(l => l with { Share = gwp > 0 ? l.Gwp / gwp * 100 : 0 }).ToList();
            return new RouteImpact
            {
                Source = source,
                SourceLabel = SourceOrDefault(source).Label,
                MassTonnes = massTonnes,
                LegCount = perLeg.Count,
                DistanceKm = perLeg.Sum(l => l.DistanceKm * l.Trips),
                Tkm = tkm,
                Gwp = gwp,
                Intensity = tkm > 0 ? gwp / tkm : 0,
                GwpPerKg = massTonnes > 0 ? gwp / (massTonnes * 1000) : 0,
                PerLeg = perLeg,
            };
        }

        /// <summary>
        /// Re-runs the route geography with every transport mode (single-mode substitution: default vehicle,
        /// fuel and load factor) and ranks the alternatives. Road distances are kept when swapping between road modes.
        /// </summary>
        public ModeComparison CompareModes(IReadOnlyList<TransportLeg> legs, string source, double massTonnes, double recommendThresholdPct = 10)
        {
            var currentGwp = CalculateRouteImpact(legs, source, massTonnes).Gwp;
            var modes = legs.Select(l => l.Mode).Distinct().ToList();
            var currentMode = modes.Count == 1 ? modes[0] : null;
            var factors = Factors(source);

            var ranked = Ref.TransportModes
                .Where(factors.ContainsKey)
                .Select(mode =>
                {
                    var swapped = legs.Select(l => new TransportLeg
                    {
                        From = l.From,
                        To = l.To,
                        Mode = mode,
                        Load = Ref.DefaultLoadFor(mode),
                        Ret = l.Ret,
                        RoadKm = Ref.IsRoadMode(mode) && Ref.IsRoadMode(l.Mode) ? l.RoadKm : null,
                    }).ToList();
                    var impact = CalculateRouteImpact(swapped, source, massTonnes);
                    var delta = currentGwp > 0 ? (impact.Gwp - currentGwp) / currentGwp * 100 : 0;
                    return new ModeAlternative(mode, factors[mode].Label, impact.Gwp, impact.Intensity, impact.DistanceKm, delta, mode == currentMode);
                })
                .OrderBy(a => a.Gwp)
                .ToList();

            var best = ranked.FirstOrDefault();
            var worst = ranked.LastOrDefault();
            var recommendations = ranked
                .Where(c => currentGwp > 0 && c.Mode != currentMode && (1 - c.Gwp / currentGwp) * 100 >= recommendThresholdPct)
                .Select(c =>
                {
                    var reduction = (1 - c.Gwp / currentGwp) * 100;
                    return new ModeRecommendation(c.Mode, c.ModeLabel, c.Gwp, reduction,
                        $"Switching to {c.ModeLabel} would cut this route's GWP by {Fmt(reduction, 0)}% ({Fmt(currentGwp - c.Gwp)} kg CO₂-eq).");
                })
                .ToList();

            return new ModeComparison
            {
                Current = new CurrentMode(currentMode, currentMode is null ? "Mixed / multi-mode" : factors[currentMode].Label, currentGwp),
                Best = best,
                Worst = worst,
                SavingsVsWorst = worst is { Gwp: > 0 } && best is not null ? (1 - best.Gwp / worst.Gwp) * 100 : 0,
                SavingsVsCurrent = currentGwp > 0 && best is not null ? (1 - best.Gwp / currentGwp) * 100 : 0,
                IsCurrentBest = currentMode is not null && best?.Mode == currentMode,
                Ranked = ranked,
                Recommendations = recommendations,
            };
        }

        public RouteStatistics BuildStatistics(RouteImpact impact)
        {
            var rows = impact.PerLeg;
            var totalDistance = rows.Sum(r => r.DistanceKm * r.Trips);
            var byMode = rows
                .GroupBy(r => r.ModeLabel)
                .Select(g => new ModeShare(g.Key, g.Sum(r => r.Gwp), impact.Gwp > 0 ? g.Sum(r => r.Gwp) / impact.Gwp * 100 : 0))
                .OrderByDescending(m => m.Gwp)
                .ToList();
            var top = rows.OrderByDescending(r => r.Gwp).FirstOrDefault();

            return new RouteStatistics(
                new RouteTotals
                {
                    Gwp = impact.Gwp,
                    DistanceKm = totalDistance,
                    Tkm = impact.Tkm,
                    Intensity = impact.Intensity,
                    GwpPerKm = totalDistance > 0 ? impact.Gwp / totalDistance : 0,
                    GwpPerKg = impact.GwpPerKg,
                    LegCount = rows.Count,
                    ModeCount = byMode.Count,
                    MeanPerLeg = rows.Count > 0 ? impact.Gwp / rows.Count : 0,
                    TopLegKey = top?.Key,
                },
                byMode,
                Describe(rows.Select(r => r.Gwp).ToList()),
                Describe(rows.Select(r => r.DistanceKm * r.Trips).ToList()),
                Describe(rows.Select(r => r.EffectiveEf).ToList()));
        }

        public IReadOnlyList<IndicatorResult> BuildIndicators(RouteImpact impact, IReadOnlyList<TransportLeg> legs, string source)
        {
            Ref.IndicatorFactors.TryGetValue(source, out var table);
            return Ref.En15804CoreIndicators.Select(ind =>
            {
                if (ind.Key == "gwp-total") return new IndicatorResult(ind.Key, ind.Abbr, ind.Name, ind.Unit, impact.Gwp);

                double? sum = legs.Count > 0 ? 0 : null;
                for (var i = 0; i < legs.Count && sum is not null; i++)
                {
                    if (table is not null && table.TryGetValue(legs[i].Mode, out var modeFactors) && modeFactors.TryGetValue(ind.Key, out var f))
                        sum += impact.PerLeg[i].Tkm * f;
                    else
                        sum = null;
                }
                return new IndicatorResult(ind.Key, ind.Abbr, ind.Name, ind.Unit, sum);
            }).ToList();
        }

        public IReadOnlyList<TransportLeg> RouteLegs(TransportState state) =>
            state.Multi
                ? state.Legs
                :
                [
                    new TransportLeg
                    {
                        Id = "direct",
                        From = state.From,
                        To = state.To,
                        Mode = state.Mode,
                        Load = state.LoadFactor,
                        ManualDist = state.ManualDist,
                        DistUnit = state.DistUnit,
                        RoadKm = state.RoadKm,
                    },
                ];

        public ScenarioResult Calculate(TransportState state)
        {
            var source = Ref.EmissionFactorSources.ContainsKey(state.Source) ? state.Source : Ref.DefaultSource;
            var cargo = SummarizeCargo(state.CargoItems);
            var legs = RouteLegs(state);
            var impact = CalculateRouteImpact(legs, source, cargo.TotalTonnes);
            var (routeFrom, routeTo) = state.Multi && legs.Count > 0 ? (legs[0].From, legs[^1].To) : (state.From, state.To);

            return new ScenarioResult
            {
                Cargo = cargo,
                Validation = ValidateRoute(routeFrom, routeTo),
                GreatCircleKm = GeoMath.Haversine(state.From, state.To),
                Impact = impact,
                Comparison = CompareModes(legs, source, cargo.TotalTonnes),
                Statistics = BuildStatistics(impact),
                Indicators = BuildIndicators(impact, legs, source),
            };
        }

        private static DescriptiveStats Describe(IReadOnlyList<double> values)
        {
            var n = values.Count;
            if (n == 0) return new DescriptiveStats(0, 0, 0, 0, 0, 0, 0, 0);
            var sorted = values.OrderBy(v => v).ToList();
            var sum = values.Sum();
            var mean = sum / n;
            var median = n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2;
            var std = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / n);
            return new DescriptiveStats(n, sorted[0], sorted[^1], mean, median, std, sum, mean != 0 ? std / mean : 0);
        }

        private static EmissionFactorSource SourceOrDefault(string source) =>
            Ref.EmissionFactorSources.TryGetValue(source, out var s) ? s : Ref.EmissionFactorSources[Ref.DefaultSource];

        private static IReadOnlyDictionary<string, ModeFactor> Factors(string source) => SourceOrDefault(source).Factors;
    }
}
