using TransportGwp.Application.Implementation;
using TransportGwp.Domain.Models;
using TransportGwp.Infrastructure.Geo;

namespace TransportGwp.Tests
{
    public class ExportAndParsingTests
    {
        [Fact]
        public void Export_ProducesReportCsvAndJson()
        {
            var state = new TransportState
            {
                CargoItems = [new() { Material = "concrete", Mass = 10 }],
                From = new() { Label = "Oslo, Norway", Lat = 59.913, Lon = 10.752 },
                To = new() { Label = "Hamburg, Germany", Lat = 53.551, Lon = 9.993 },
                Mode = "truck-large",
                LoadFactor = 100,
                RoadKm = 1000,
            };

            var bundle = new ExportService(new TransportCalculator()).Export(state);

            Assert.Contains("TOTAL", bundle.Report);
            Assert.Contains("800 kg CO₂-eq", bundle.Report);
            Assert.StartsWith("leg,mode,from,to,distance_km,tkm,gwp_kgco2e,return", bundle.Csv);
            Assert.Contains("\"Oslo, Norway\"", bundle.Csv); // commas are quoted
            Assert.Contains("\"gwp\": 800", bundle.Json);
        }

        [Fact]
        public void PolylineDecoder_DecodesGoogleExample()
        {
            // Example from Google's encoded polyline documentation
            var points = PolylineDecoder.Decode("_p~iF~ps|U_ulLnnqC_mqNvxq`@");

            Assert.Equal(3, points.Count);
            Assert.Equal(38.5, points[0][0], 5);
            Assert.Equal(-120.2, points[0][1], 5);
            Assert.Equal(43.252, points[2][0], 5);
            Assert.Equal(-126.453, points[2][1], 5);
        }

        [Theory]
        [InlineData("```json\n{\"headline\":\"x\",\"suggestions\":[\"a\"]}\n```")]
        [InlineData("Here you go: {\"headline\":\"x\",\"suggestions\":[\"a\"]} Hope it helps")]
        public void ExtractJson_ToleratesFencesAndProse(string reply)
        {
            var json = AiAdvisor.ExtractJson(reply);

            Assert.NotNull(json);
            Assert.Equal("x", json!.Value.GetProperty("headline").GetString());
        }

        [Theory]
        [InlineData(1234.56, 1, "1,235")]
        [InlineData(12.34, 1, "12.3")]
        [InlineData(1.23456, 3, "1.235")]
        [InlineData(2.0, 2, "2")]
        public void NumberFormat_MatchesReportConventions(double n, int d, string expected)
        {
            Assert.Equal(expected, NumberFormat.Fmt(n, d));
        }
    }
}
