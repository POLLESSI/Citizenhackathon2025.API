using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.AI.FastApi
{
    /// <summary>
    /// Contract returned by OutZen.AI / FastAPI.
    /// </summary>
    public sealed class FastApiGenerationResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; init; }
            = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; init; }
            = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; init; }
            = string.Empty;
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.