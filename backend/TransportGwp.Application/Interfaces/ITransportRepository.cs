using TransportGwp.Domain.Entities;

namespace TransportGwp.Application.Interfaces
{
    public interface ITransportRepository
    {
        Task<IReadOnlyList<Transport>> GetAll(CancellationToken ct);
        Task<Transport?> Get(string id, CancellationToken ct);
        Task<bool> NameExists(string name, CancellationToken ct);
        Task Add(Transport transport, CancellationToken ct);
        Task Update(Transport transport, CancellationToken ct);
        Task<bool> Delete(string id, CancellationToken ct);
    }
}
