using TransportGwp.Application.Implementation;
using TransportGwp.Domain.Models;

namespace TransportGwp.Tests
{
    public class TransportCalculatorTests
    {
        private readonly TransportCalculator _calculator = new();

        private static readonly GeoLocation MoIRana = new() { Label = "Mo i Rana, Norway", Lat = 66.314, Lon = 14.142 };
        private static readonly GeoLocation Oslo = new() { Label = "Oslo, Norway", Lat = 59.913, Lon = 10.752 };
        private static readonly GeoLocation Hamburg = new() { Label = "Hamburg, Germany", Lat = 53.551, Lon = 9.993 };

        [Theory]
        [InlineData(100, 1.0)]
        [InlineData(61, 1.2804)]
        [InlineData(25, 2.0)]
        [InlineData(5, 2.2)]   // clamped: load < 15% treated as 15%, result capped at 2.2
        [InlineData(150, 1.0)] // clamped to 100%
        public void LoadAdjustment_FollowsSquareRootRuleWithinBounds(double load, double expected)
        {
            Assert.Equal(expected, TransportCalculator.LoadAdjustment(load), 4);
        }

        [Fact]
        public void CalculateLeg_UsesEnteredDistanceForNonRoadMode()
        {
            var leg = new TransportLeg { From = MoIRana, To = Oslo, Mode = "rail", Load = 65, ManualDist = 100 };

            var r = _calculator.CalculateLeg(leg, "en15804", massTonnes: 10);

            Assert.Equal("manual", r.DistanceSource);
            Assert.Equal(100, r.DistanceKm, 6);
            // 10 t × 100 km × 0.025 × √(100/65)
            Assert.Equal(10 * 100 * 0.025 * Math.Sqrt(100.0 / 65), r.Gwp, 6);
        }

        [Fact]
        public void CalculateLeg_ConvertsMilesToKm()
        {
            var leg = new TransportLeg { Mode = "sea-container", Load = 100, ManualDist = 100, DistUnit = "mi" };

            var r = _calculator.CalculateLeg(leg, "en15804", 1);

            Assert.Equal(160.9344, r.DistanceKm, 4);
        }

        [Fact]
        public void CalculateLeg_IgnoresEnteredDistanceForRoadModeAndUsesGoogleRoadKm()
        {
            var leg = new TransportLeg { From = MoIRana, To = Oslo, Mode = "truck-large", Load = 100, ManualDist = 5, RoadKm = 1000 };

            var r = _calculator.CalculateLeg(leg, "en15804", 2);

            Assert.Equal("road", r.DistanceSource);
            Assert.Equal(1000, r.DistanceKm);
            Assert.Equal(2 * 1000 * 0.080, r.Gwp, 6);
        }

        [Fact]
        public void CalculateLeg_EstimatesWithRoutingFactorWhenNoDistanceKnown()
        {
            var leg = new TransportLeg { From = MoIRana, To = Oslo, Mode = "truck-large", Load = 100 };

            var r = _calculator.CalculateLeg(leg, "en15804", 1);

            Assert.Equal("estimate", r.DistanceSource);
            Assert.Equal(r.GreatCircleKm * 1.25, r.DistanceKm, 6);
            Assert.InRange(r.GreatCircleKm, 700, 760);
        }

        [Fact]
        public void CalculateLeg_AppliesVehicleFuelAndReturnTrip()
        {
            var leg = new TransportLeg { Mode = "truck-large", Vehicle = "euro6", Fuel = "hvo100", Load = 100, Ret = true, RoadKm = 100 };

            var r = _calculator.CalculateLeg(leg, "en15804", 1);

            Assert.Equal(0.93, r.VehicleMult);
            Assert.Equal(0.20, r.FuelMult);
            Assert.Equal(2, r.Trips);
            Assert.Equal(1 * 100 * 0.080 * 0.93 * 0.20 * 2, r.Gwp, 6);
            Assert.Equal(200, r.Tkm, 6);
        }

        [Fact]
        public void CalculateRouteImpact_SumsLegsAndComputesIntensityAndShares()
        {
            var legs = new List<TransportLeg>
            {
                new() { Mode = "truck-large", Load = 100, RoadKm = 100 },
                new() { Mode = "rail", Load = 100, ManualDist = 400 },
            };

            var impact = _calculator.CalculateRouteImpact(legs, "en15804", 10);

            var expectedGwp = 10 * 100 * 0.080 + 10 * 400 * 0.025;
            Assert.Equal(expectedGwp, impact.Gwp, 6);
            Assert.Equal(500, impact.DistanceKm, 6);
            Assert.Equal(5000, impact.Tkm, 6);
            Assert.Equal(expectedGwp / 5000, impact.Intensity, 8);
            Assert.Equal(expectedGwp / 10000, impact.GwpPerKg, 8);
            Assert.Equal(100, impact.PerLeg.Sum(l => l.Share), 6);
        }

        [Fact]
        public void SummarizeCargo_ConvertsVolumeWithDensityAndGroupsByMaterial()
        {
            var items = new List<CargoItem>
            {
                new() { Material = "concrete", Mass = 18 },
                new() { Material = "steel", ByVolume = true, Volume = 2 }, // 2 m³ × 7850 kg/m³ = 15.7 t
                new() { Material = "concrete", Mass = 2 },
            };

            var summary = _calculator.SummarizeCargo(items);

            Assert.Equal(35.7, summary.TotalTonnes, 6);
            Assert.Equal("Concrete", summary.ByMaterial[0].Name);
            Assert.Equal(20, summary.ByMaterial[0].Tonnes, 6);
            Assert.Equal("2 materials (Concrete, Steel)", summary.Label);
        }

        [Fact]
        public void ValidateRoute_ReportsMissingPointsAsErrors()
        {
            var v = _calculator.ValidateRoute(MoIRana, null);

            Assert.False(v.Valid);
            Assert.Equal("error", v.Level);
        }

        [Fact]
        public void ValidateRoute_WarnsWhenPointsAreAlmostIdentical()
        {
            var v = _calculator.ValidateRoute(Oslo, new GeoLocation { Label = "Oslo", Lat = 59.9131, Lon = 10.7521 });

            Assert.True(v.Valid);
            Assert.Equal("warning", v.Level);
        }

        [Fact]
        public void CompareModes_RanksAscendingAndRecommendsLowerCarbonModes()
        {
            var legs = new List<TransportLeg> { new() { From = Oslo, To = Hamburg, Mode = "truck-large", Load = 61 } };

            var cmp = _calculator.CompareModes(legs, "en15804", 24);

            Assert.Equal(cmp.Ranked.OrderBy(r => r.Gwp).Select(r => r.Mode), cmp.Ranked.Select(r => r.Mode));
            Assert.Equal("sea-container", cmp.Best!.Mode);
            Assert.Equal("air", cmp.Worst!.Mode);
            Assert.Equal("truck-large", cmp.Current.Mode);
            Assert.Contains(cmp.Recommendations, r => r.Mode == "rail");
            Assert.DoesNotContain(cmp.Recommendations, r => r.Mode == "air");
            Assert.True(cmp.Ranked.Single(r => r.Mode == "truck-large").IsCurrent);
        }

        [Fact]
        public void Calculate_SingleRouteUsesGoogleRoadKmEverywhere()
        {
            var state = new TransportState
            {
                CargoItems = [new() { Material = "concrete", Mass = 10 }],
                From = MoIRana,
                To = Oslo,
                Mode = "truck-large",
                LoadFactor = 100,
                RoadKm = 1000,
            };

            var r = _calculator.Calculate(state);

            Assert.Equal(10 * 1000 * 0.080, r.Impact.Gwp, 6);
            Assert.Equal(r.Impact.Gwp, r.Statistics.Total.Gwp, 6);
            Assert.Equal(r.Impact.Gwp, r.Indicators.Single(i => i.Key == "gwp-total").Value!.Value, 6);
            Assert.All(r.Indicators.Where(i => i.Key != "gwp-total"), i => Assert.Null(i.Value));
        }

        [Fact]
        public void BuildStatistics_DescribesSpreadAcrossLegs()
        {
            var legs = new List<TransportLeg>
            {
                new() { Mode = "truck-large", Load = 100, RoadKm = 100 },  // 8 kg
                new() { Mode = "truck-large", Load = 100, RoadKm = 300 },  // 24 kg
            };
            var impact = _calculator.CalculateRouteImpact(legs, "en15804", 1);

            var s = _calculator.BuildStatistics(impact);

            Assert.Equal(16, s.GwpSpread.Mean, 6);
            Assert.Equal(8, s.GwpSpread.Std, 6);
            Assert.Equal("L2", s.Total.TopLegKey);
            Assert.Single(s.ModeShares);
        }
    }
}
