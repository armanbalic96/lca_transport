using System.Globalization;

namespace TransportGwp.Application.Implementation
{
    /// <summary>Number formatting used in report text: ≥1000 → no decimals, ≥10 → one decimal, else up to <c>decimals</c>.</summary>
    public static class NumberFormat
    {
        private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        public static string Fmt(double n, int decimals = 1)
        {
            if (double.IsNaN(n) || double.IsInfinity(n)) return "—";
            var abs = Math.Abs(n);
            if (abs >= 1000) return n.ToString("#,##0", En);
            if (abs >= 10) return n.ToString("#,##0.#", En);
            return n.ToString("#,##0." + new string('#', Math.Max(0, decimals)), En).TrimEnd('.');
        }
    }
}
