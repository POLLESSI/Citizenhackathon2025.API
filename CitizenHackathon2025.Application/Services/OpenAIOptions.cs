
namespace Citizenhackathon2025.Application.Services
{
    public class OpenAIOptions
    {
    #nullable disable
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string Model { get; set; }
    }
}
