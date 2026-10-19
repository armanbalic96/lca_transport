using Moq;
using TransportGwp.Application.Implementation;
using TransportGwp.Application.Interfaces;
using TransportGwp.Domain.Entities;
using TransportGwp.Domain.Models;

namespace TransportGwp.Tests
{
    public class TransportRegisterServiceTests
    {
        private readonly Mock<ITransportRepository> _repo = new();
        private readonly TransportRegisterService _service;

        public TransportRegisterServiceTests()
            => _service = new TransportRegisterService(_repo.Object, new TransportCalculator(), TimeProvider.System);

        [Fact]
        public async Task Create_RejectsDuplicateName()
        {
            _repo.Setup(r => r.NameExists("Precast", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<DuplicateNameException>(() => _service.Create("  Precast ", CancellationToken.None));
            _repo.Verify(r => r.Add(It.IsAny<Transport>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_RejectsEmptyName()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.Create("   ", CancellationToken.None));
        }

        [Fact]
        public async Task SaveState_RecomputesSummaryOnServer()
        {
            var transport = new Transport { Id = "t1", Name = "Test" };
            _repo.Setup(r => r.Get("t1", It.IsAny<CancellationToken>())).ReturnsAsync(transport);
            var state = new TransportState
            {
                MaxReached = 2,
                CargoItems = [new() { Mass = 5 }],
                From = new() { Label = "A", Lat = 60, Lon = 10 },
                To = new() { Label = "B", Lat = 61, Lon = 10 },
                Mode = "truck-large",
                LoadFactor = 100,
                RoadKm = 200,
            };

            var item = await _service.SaveState("t1", state, CancellationToken.None);

            Assert.NotNull(item?.Summary);
            Assert.Equal(5 * 200 * 0.080, item!.Summary!.Gwp!.Value, 6);
            Assert.Equal("Truck >16t", item.Summary.ModeLabel);
            Assert.Equal(2, item.Summary.MaxReached);
            Assert.NotNull(transport.StateJson);
            _repo.Verify(r => r.Update(transport, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Duplicate_PicksNextFreeName()
        {
            _repo.Setup(r => r.Get("t1", It.IsAny<CancellationToken>())).ReturnsAsync(new Transport { Id = "t1", Name = "Route" });
            _repo.Setup(r => r.NameExists("Route (2)", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var copy = await _service.Duplicate("t1", CancellationToken.None);

            Assert.Equal("Route (3)", copy!.Name);
        }
    }
}
