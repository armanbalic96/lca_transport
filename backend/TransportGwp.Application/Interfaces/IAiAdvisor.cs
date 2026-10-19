using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Application.Interfaces
{
    /// <summary>Sends one prompt to a language model and returns the text reply.</summary>
    public interface ILlmClient
    {
        bool IsConfigured { get; }
        Task<string> Complete(string prompt, CancellationToken ct);
    }

    /// <summary>AI decarbonisation advice and explanations for a calculated scenario.</summary>
    public interface IAiAdvisor
    {
        bool IsAvailable { get; }
        Task<AiRecommendation> Recommend(TransportState state, CancellationToken ct);
        Task<string> Explain(string question, TransportState state, CancellationToken ct);
    }

    public class AiUnavailableException(string message) : Exception(message);
}
