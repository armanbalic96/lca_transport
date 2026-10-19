using System.Text.Json;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Entities;
using TransportGwp.Domain.Models;
using Ref = TransportGwp.Domain.ReferenceData.TransportReferenceData;

namespace TransportGwp.Application.Implementation
{
    public class TransportRegisterService : ITransportRegisterService
    {
        private const int MaxNameLength = 120;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ITransportRepository _repository;
        private readonly ITransportCalculator _calculator;
        private readonly TimeProvider _clock;

        public TransportRegisterService(ITransportRepository repository, ITransportCalculator calculator, TimeProvider clock)
        {
            _repository = repository;
            _calculator = calculator;
            _clock = clock;
        }

        public async Task<IReadOnlyList<TransportListItem>> List(CancellationToken ct)
        {
            var all = await _repository.GetAll(ct);
            return all.OrderByDescending(t => t.UpdatedAt).Select(ToListItem).ToList();
        }

        public async Task<TransportDetail?> Get(string id, CancellationToken ct)
        {
            var t = await _repository.Get(id, ct);
            return t is null ? null : ToDetail(t);
        }

        public async Task<TransportDetail> Create(string name, CancellationToken ct)
        {
            var trimmed = (name ?? "").Trim();
            if (trimmed.Length == 0 || trimmed.Length > MaxNameLength)
                throw new ArgumentException($"Name must be 1–{MaxNameLength} characters.", nameof(name));
            if (await _repository.NameExists(trimmed, ct))
                throw new DuplicateNameException(trimmed);

            var now = _clock.GetUtcNow().UtcDateTime;
            var transport = new Transport { Id = NewId(), Name = trimmed, CreatedAt = now, UpdatedAt = now };
            await _repository.Add(transport, ct);
            return ToDetail(transport);
        }

        public async Task<TransportListItem?> SaveState(string id, TransportState state, CancellationToken ct)
        {
            var transport = await _repository.Get(id, ct);
            if (transport is null) return null;

            var impact = _calculator.Calculate(state).Impact;
            var legs = state.Legs;
            transport.StateJson = JsonSerializer.Serialize(state, JsonOptions);
            transport.FromLabel = state.Multi ? legs.FirstOrDefault()?.From?.Label : state.From?.Label;
            transport.ToLabel = state.Multi ? legs.LastOrDefault()?.To?.Label : state.To?.Label;
            transport.Mode = state.Multi ? null : state.Mode;
            transport.ModeLabel = state.Multi
                ? $"Multi-leg · {legs.Count} leg{(legs.Count != 1 ? "s" : "")}"
                : Ref.EmissionFactorSources[Ref.DefaultSource].Factors.GetValueOrDefault(state.Mode)?.Label ?? state.Mode;
            transport.MassTonnes = impact.MassTonnes;
            transport.Gwp = impact.Gwp;
            transport.DistanceKm = impact.DistanceKm;
            transport.MaxReached = state.MaxReached;
            transport.UpdatedAt = _clock.GetUtcNow().UtcDateTime;

            await _repository.Update(transport, ct);
            return ToListItem(transport);
        }

        public async Task<TransportListItem?> Duplicate(string id, CancellationToken ct)
        {
            var src = await _repository.Get(id, ct);
            if (src is null) return null;

            var n = 2;
            string name;
            do { name = $"{src.Name} ({n++})"; } while (await _repository.NameExists(name, ct));

            var now = _clock.GetUtcNow().UtcDateTime;
            var copy = new Transport
            {
                Id = NewId(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
                StateJson = src.StateJson,
                FromLabel = src.FromLabel,
                ToLabel = src.ToLabel,
                Mode = src.Mode,
                ModeLabel = src.ModeLabel,
                MassTonnes = src.MassTonnes,
                Gwp = src.Gwp,
                DistanceKm = src.DistanceKm,
                MaxReached = src.MaxReached,
            };
            await _repository.Add(copy, ct);
            return ToListItem(copy);
        }

        public Task<bool> Delete(string id, CancellationToken ct) => _repository.Delete(id, ct);

        private static string NewId() => "t" + Guid.NewGuid().ToString("N")[..12];

        private static TransportListItem ToListItem(Transport t) => new(
            t.Id, t.Name, t.CreatedAt, t.UpdatedAt,
            t.StateJson is null
                ? null
                : new TransportSummary(t.FromLabel, t.ToLabel, t.Mode, t.ModeLabel, t.MassTonnes, t.Gwp, t.DistanceKm, t.MaxReached));

        private static TransportDetail ToDetail(Transport t) => new(
            t.Id, t.Name, t.CreatedAt, t.UpdatedAt,
            t.StateJson is null ? null : JsonSerializer.Deserialize<TransportState>(t.StateJson, JsonOptions));
    }
}
