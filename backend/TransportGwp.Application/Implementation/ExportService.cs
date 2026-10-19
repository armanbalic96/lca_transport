using System.Globalization;
using System.Text;
using System.Text.Json;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;
using static TransportGwp.Application.Implementation.NumberFormat;

namespace TransportGwp.Application.Implementation
{
    /// <summary>Builds the plain-text report, CSV and JSON exports of a scenario for reports and EPD documentation.</summary>
    public class ExportService : IExportService
    {
        public const string Standard = "EN 15804+A2";
        public const string Indicator = "GWP-total (kg CO₂-eq, GWP100, IPCC AR6)";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        private readonly ITransportCalculator _calculator;

        public ExportService(ITransportCalculator calculator)
            => _calculator = calculator;

        public ExportBundle Export(TransportState state, string? name = null)
        {
            var result = _calculator.Calculate(state);
            var impact = result.Impact;
            var title = string.IsNullOrWhiteSpace(name) ? "Transport scenario" : name;

            var data = new
            {
                name = title,
                generatedAt = DateTime.UtcNow,
                standard = Standard,
                indicator = Indicator,
                efSource = impact.SourceLabel,
                functionalUnit = new { massTonnes = impact.MassTonnes, massKg = impact.MassTonnes * 1000, cargo = result.Cargo.Label },
                route = new { distanceKm = impact.DistanceKm, tkm = impact.Tkm, intensity = impact.Intensity, legs = impact.PerLeg },
                totals = new { gwp = impact.Gwp, gwpPerKg = impact.GwpPerKg, distanceKm = impact.DistanceKm },
            };

            return new ExportBundle(BuildReport(title, result), BuildCsv(impact), JsonSerializer.Serialize(data, JsonOptions));
        }

        private static string BuildReport(string title, ScenarioResult result)
        {
            var impact = result.Impact;
            static string Pad(string k, string v) => $"{k,-28} {v}";
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine(new string('=', Math.Max(12, title.Length)));
            sb.AppendLine();
            sb.AppendLine(Pad("Standard", Standard));
            sb.AppendLine(Pad("Indicator", Indicator));
            sb.AppendLine(Pad("Emission factor source", impact.SourceLabel));
            sb.AppendLine(Pad("Functional unit", $"{Fmt(impact.MassTonnes, 2)} t · {result.Cargo.Label}"));
            sb.AppendLine();
            sb.AppendLine("Route");
            if (impact.PerLeg.Count == 0) sb.AppendLine("   (no legs)");
            foreach (var l in impact.PerLeg)
            {
                sb.AppendLine($"   {l.Key}  {l.ModeLabel,-16} {Fmt(l.DistanceKm, 0),6} km   {Fmt(l.Gwp),9} kg CO₂e{(l.Ret ? "  (incl. return)" : "")}");
            }
            sb.AppendLine();
            sb.AppendLine(new string('-', 48));
            sb.AppendLine(Pad("TOTAL", $"{Fmt(impact.Gwp)} kg CO₂-eq"));
            sb.Append(Pad("Per kg of product", $"{Fmt(impact.GwpPerKg * 1000, 3)} g CO₂-eq/kg"));
            return sb.ToString();
        }

        private static string BuildCsv(RouteImpact impact)
        {
            var inv = CultureInfo.InvariantCulture;
            var rows = new List<string[]> { new[] { "leg", "mode", "from", "to", "distance_km", "tkm", "gwp_kgco2e", "return" } };
            rows.AddRange(impact.PerLeg.Select(l => new[]
            {
                l.Key, l.ModeLabel, l.From ?? "", l.To ?? "",
                Math.Round(l.DistanceKm, 1).ToString(inv), Math.Round(l.Tkm, 3).ToString(inv), Math.Round(l.Gwp, 3).ToString(inv),
                l.Ret ? "yes" : "no",
            }));
            rows.Add([]);
            rows.Add(["TOTAL", "", "", "", Math.Round(impact.DistanceKm, 1).ToString(inv), Math.Round(impact.Tkm, 3).ToString(inv), Math.Round(impact.Gwp, 3).ToString(inv), ""]);
            return string.Join("\n", rows.Select(r => string.Join(",", r.Select(Escape))));

            static string Escape(string s) => s.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        }
    }
}
