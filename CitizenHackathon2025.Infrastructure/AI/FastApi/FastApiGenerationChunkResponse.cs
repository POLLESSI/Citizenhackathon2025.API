using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.AI.FastApi
{
    public sealed class FastApiGenerationChunkResponse
    {
        [JsonPropertyName("chunk")]
        public string Chunk { get; init; }
            = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; init; }

        [JsonPropertyName("model")]
        public string Model { get; init; }
            = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; init; }
            = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.