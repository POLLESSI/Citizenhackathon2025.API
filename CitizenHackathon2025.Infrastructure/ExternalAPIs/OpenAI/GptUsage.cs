namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI
{
    public sealed class GptUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
