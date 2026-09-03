using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.AI.FastApi
{
    /// <summary>
    /// Contract sent by ASP.NET Core to OutZen.AI / FastAPI.
    /// </summary>
    public sealed class FastApiGenerationRequest
    {
        [JsonPropertyName("grounded_prompt")]
        public string GroundedPrompt { get; init; }
            = string.Empty;

        [JsonPropertyName("response_language")]
        public string ResponseLanguage { get; init; }
            = "fr-FR";

        [JsonPropertyName("temperature")]
        public double? Temperature { get; init; }
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.