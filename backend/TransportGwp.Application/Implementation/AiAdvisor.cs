using System.Text.Json;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Implementation
{
    /// <summary>
    /// Builds prompts from a server-side calculation of the scenario (the client never supplies the numbers)
    /// and asks the language model for recommendations or explanations.
    /// Only aggregated route figures are sent — no transport names or user data.
    /// </summary>
    public class AiAdvisor : IAiAdvisor
    {
        private const int MaxQuestionLength = 500;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ILlmClient _llm;
        private readonly ITransportCalculator _calculator;

        public AiAdvisor(ILlmClient llm, ITransportCalculator calculator)
        {
            _llm = llm;
            _calculator = calculator;
        }

        public bool IsAvailable => _llm.IsConfigured;

        public async Task<AiRecommendation> Recommend(TransportState state, CancellationToken ct)
        {
            var prompt =
                "You are an LCA decarbonization advisor for construction transport (EN 15804 A2/A4, indicator: kg CO2-eq).\n" +
                "Using ONLY the data below, write a one-line headline and exactly 3 concrete, quantified suggestions to cut transport GWP " +
                "(switch mode, fuel, load factor, or route). Prefer the cheapest realistic wins first and cite approximate kg or % from the data.\n" +
                "Return JSON ONLY: {\"headline\":\"...\",\"suggestions\":[\"...\",\"...\",\"...\"]}\n\n" +
                $"Data: {BuildContext(state)}";

            var raw = await Complete(prompt, ct);
            var parsed = ExtractJson(raw) ?? throw new AiUnavailableException("Could not generate recommendations.");
            var headline = parsed.TryGetProperty("headline", out var h) ? h.GetString() ?? "" : "";
            var suggestions = parsed.TryGetProperty("suggestions", out var s) && s.ValueKind == JsonValueKind.Array
                ? s.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).Take(4).ToList()
                : [];
            return new AiRecommendation(headline, suggestions);
        }

        public async Task<string> Explain(string question, TransportState state, CancellationToken ct)
        {
            var q = (question ?? "").Trim();
            if (q.Length == 0) throw new ArgumentException("Question is required.", nameof(question));
            if (q.Length > MaxQuestionLength) q = q[..MaxQuestionLength];

            var prompt =
                "You are an LCA transport expert. Answer the user's question about THIS specific calculation in 2-4 short sentences of plain language. " +
                "No markdown headers, no bullet symbols. Be concrete and use the numbers from the data. If the question asks about assumptions, mention: " +
                "great-circle distance × a mode routing factor (or the Google road distance / entered distance where used), the chosen emission-factor database, " +
                "load-factor adjustment, and optional return trips.\n\n" +
                $"Question: \"{q.Replace("\"", "'")}\"\n" +
                $"Data: {BuildContext(state)}";

            return (await Complete(prompt, ct)).Trim();
        }

        private async Task<string> Complete(string prompt, CancellationToken ct)
        {
            if (!_llm.IsConfigured) throw new AiUnavailableException("AI is not configured on the server.");
            return await _llm.Complete(prompt, ct);
        }

        private string BuildContext(TransportState state)
        {
            var r = _calculator.Calculate(state);
            var impact = r.Impact;
            var ctx = new
            {
                standard = "EN 15804+A2",
                indicator = "GWP kg CO2-eq",
                efSource = impact.SourceLabel,
                massTonnes = Math.Round(impact.MassTonnes, 3),
                material = r.Cargo.Label,
                cargo = r.Cargo.ByMaterial.Select(m => new { material = m.Name, tonnes = Math.Round(m.Tonnes, 3) }),
                route = new
                {
                    gwp = Math.Round(impact.Gwp, 2),
                    distanceKm = Math.Round(impact.DistanceKm),
                    intensity = Math.Round(impact.Intensity, 5),
                    legs = impact.PerLeg.Select(l => new
                    {
                        mode = l.ModeLabel, from = l.From, to = l.To, km = Math.Round(l.DistanceKm),
                        distanceSource = l.DistanceSource, load = l.Load, gwp = Math.Round(l.Gwp, 1), ret = l.Ret,
                    }),
                },
                total = new { gwp = Math.Round(impact.Gwp, 2), gwpPerKg = Math.Round(impact.GwpPerKg, 5) },
                alternatives = r.Comparison.Ranked.Take(5).Select(c => new { mode = c.ModeLabel, gwp = Math.Round(c.Gwp, 1) }),
                best = r.Comparison.Best is { } b ? new { mode = b.ModeLabel, gwp = Math.Round(b.Gwp, 1) } : null,
                savings_vs_current_pct = Math.Round(r.Comparison.SavingsVsCurrent, 1),
            };
            return JsonSerializer.Serialize(ctx, JsonOptions);
        }

        /// <summary>Extracts the first JSON object from a model reply, tolerating code fences and surrounding prose.</summary>
        public static JsonElement? ExtractJson(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            try
            {
                using var doc = JsonDocument.Parse(text[start..(end + 1)]);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
