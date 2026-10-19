using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Interfaces
{
    public interface ITransportRegisterService
    {
        Task<IReadOnlyList<TransportListItem>> List(CancellationToken ct);
        Task<TransportDetail?> Get(string id, CancellationToken ct);

        /// <exception cref="DuplicateNameException">A transport with this name already exists.</exception>
        Task<TransportDetail> Create(string name, CancellationToken ct);

        /// <summary>Stores the wizard state and recomputes the register summary server-side.</summary>
        Task<TransportListItem?> SaveState(string id, TransportState state, CancellationToken ct);

        Task<TransportListItem?> Duplicate(string id, CancellationToken ct);
        Task<bool> Delete(string id, CancellationToken ct);
    }

    public class DuplicateNameException(string name) : Exception($"A transport named '{name}' already exists.");
}
