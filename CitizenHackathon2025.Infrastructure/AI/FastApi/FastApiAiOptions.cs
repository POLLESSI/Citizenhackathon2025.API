using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.Infrastructure.AI.FastApi
{
    public sealed class FastApiAiOptions
    {
        public const string SectionName = "FastApiAI";
        [Required]
        public string BaseUrl { get; set; } = "http://127.0.0.1:8010/";
        [Required]
        public string GenerationEndpoint { get; set; } = "api/v1/generate";
        [Required]
        public string StreamingEndpoint { get; set; } = "api/v1/generate/stream";
        [Range(1, 1800)]
        public int TimeoutSeconds { get; set; } = 600;
        [Range(0.0, 2.0)]
        public double DefaultTemperature { get; set; } = 0.3;
        [Required]
        [MinLength(32)]
        public string InternalApiKey { get; set; } = string.Empty;
        
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.