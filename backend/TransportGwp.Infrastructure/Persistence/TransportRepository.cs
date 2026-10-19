using Microsoft.EntityFrameworkCore;
using TransportGwp.Application.Interfaces;
using TransportGwp.Domain.Entities;

namespace TransportGwp.Infrastructure.Persistence
{
    public class TransportRepository : ITransportRepository
    {
        private readonly TransportDbContext _db;

        public TransportRepository(TransportDbContext db)
            => _db = db;

        public async Task<IReadOnlyList<Transport>> GetAll(CancellationToken ct)
            => await _db.Transports.AsNoTracking().ToListAsync(ct);

        public Task<Transport?> Get(string id, CancellationToken ct)
            => _db.Transports.FirstOrDefaultAsync(t => t.Id == id, ct);

        public Task<bool> NameExists(string name, CancellationToken ct)
            => _db.Transports.AnyAsync(t => t.Name.ToLower() == name.ToLower(), ct);

        public async Task Add(Transport transport, CancellationToken ct)
        {
            _db.Transports.Add(transport);
            await _db.SaveChangesAsync(ct);
        }

        public async Task Update(Transport transport, CancellationToken ct)
        {
            _db.Transports.Update(transport);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> Delete(string id, CancellationToken ct)
            => await _db.Transports.Where(t => t.Id == id).ExecuteDeleteAsync(ct) > 0;
    }
}
