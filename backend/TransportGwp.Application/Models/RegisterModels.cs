using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Models
{
    public record TransportSummary(
        string? From,
        string? To,
        string? Mode,
        string? ModeLabel,
        double? MassTonnes,
        double? Gwp,
        double? DistanceKm,
        int MaxReached);

    public record TransportListItem(string Id, string Name, DateTime CreatedAt, DateTime UpdatedAt, TransportSummary? Summary);

    public record TransportDetail(string Id, string Name, DateTime CreatedAt, DateTime UpdatedAt, TransportState? State);

    public record CreateTransportRequest(string Name);
}
