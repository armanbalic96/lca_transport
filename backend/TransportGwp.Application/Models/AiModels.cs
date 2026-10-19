using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Models
{
    public record AiRecommendation(string Headline, IReadOnlyList<string> Suggestions);

    public record AiRecommendRequest(TransportState State);

    public record AiExplainRequest(string Question, TransportState State);

    public record AiExplainResponse(string Answer);

    public record AiStatus(bool Available);
}
