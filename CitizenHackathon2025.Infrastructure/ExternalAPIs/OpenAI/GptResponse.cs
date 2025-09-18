namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI
{
    public sealed class GptResponse
    {
        public GptChoice[] Choices { get; set; } = Array.Empty<GptChoice>();
        public GptUsage Usage { get; set; } = new();
    }
}
