namespace TransportGwp.Infrastructure.Ai
{
    public class ClaudeOptions
    {
        public const string Section = "Claude";

        /// <summary>Anthropic API key. Leave empty to disable the AI panels. Use user-secrets or an environment variable, never appsettings.json.</summary>
        public string? ApiKey { get; set; }

        public string Model { get; set; } = "claude-opus-5-5";

        public int MaxTokens { get; set; } = 4000;
    }
}
