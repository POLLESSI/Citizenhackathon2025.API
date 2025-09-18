namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI
{
    public sealed class GptMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }
}
