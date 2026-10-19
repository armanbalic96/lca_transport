using Anthropic;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Options;
using TransportGwp.Application.Interfaces;

namespace TransportGwp.Infrastructure.Ai
{
    /// <summary>
    /// Claude via the official Anthropic SDK. Short, single-turn requests at low effort.
    /// Server-side refusal fallback is enabled so a policy decline is re-served by another model in the same call.
    /// </summary>
    public class ClaudeLlmClient : ILlmClient
    {
        private const string FallbackBeta = "server-side-fallback-2026-07-01";

        private readonly ClaudeOptions _options;
        private readonly AnthropicClient? _client;

        public ClaudeLlmClient(IOptions<ClaudeOptions> options)
        {
            _options = options.Value;
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                _client = new AnthropicClient { ApiKey = _options.ApiKey };
        }

        public bool IsConfigured => _client is not null;

        public async Task<string> Complete(string prompt, CancellationToken ct)
        {
            if (_client is null) throw new AiUnavailableException("AI is not configured on the server.");

            var response = await _client.Beta.Messages.Create(new MessageCreateParams
            {
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Betas = [FallbackBeta],
                Fallbacks = new Default(),
                OutputConfig = new BetaOutputConfig { Effort = Effort.Low },
                Messages = [new() { Role = Role.User, Content = prompt }],
            }, ct);

            if (response.StopReason == "refusal")
                throw new AiUnavailableException("The AI declined to answer this request.");

            return string.Concat(response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
        }
    }
}
