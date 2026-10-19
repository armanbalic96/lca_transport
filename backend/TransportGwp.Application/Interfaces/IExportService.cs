using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Interfaces
{
    public interface IExportService
    {
        ExportBundle Export(TransportState state, string? name = null);
    }
}
