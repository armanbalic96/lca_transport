using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Implementation
{
    public static class GeoMath
    {
        private const double EarthRadiusKm = 6371;

        /// <summary>Great-circle distance in km. Returns 0 when either point is missing.</summary>
        public static double Haversine(GeoLocation? a, GeoLocation? b)
        {
            if (a is null || b is null) return 0;
            static double ToRad(double d) => d * Math.PI / 180;
            var dLat = ToRad(b.Lat - a.Lat);
            var dLon = ToRad(b.Lon - a.Lon);
            var lat1 = ToRad(a.Lat);
            var lat2 = ToRad(b.Lat);
            var x = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
            return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(x));
        }
    }
}
