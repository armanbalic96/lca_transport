namespace TransportGwp.Infrastructure.Geo
{
    /// <summary>Decodes Google's encoded polyline format into [lat, lon] pairs.</summary>
    public static class PolylineDecoder
    {
        public static IReadOnlyList<double[]> Decode(string? encoded)
        {
            var points = new List<double[]>();
            if (string.IsNullOrEmpty(encoded)) return points;

            int index = 0, lat = 0, lng = 0;
            while (index < encoded.Length)
            {
                lat += NextValue(encoded, ref index);
                if (index >= encoded.Length) break;
                lng += NextValue(encoded, ref index);
                points.Add([lat / 1e5, lng / 1e5]);
            }
            return points;
        }

        private static int NextValue(string encoded, ref int index)
        {
            int result = 0, shift = 0, b;
            do
            {
                b = encoded[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20 && index < encoded.Length);
            return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
        }
    }
}
