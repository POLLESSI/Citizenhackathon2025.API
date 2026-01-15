using Polly;
using System.Net.Http;

namespace CitizenHackathon2025.Infrastructure.Resilience;

public sealed class ResiliencePipelines
{
    public required ResiliencePipeline<HttpResponseMessage> OpenAi { get; init; }
    public required ResiliencePipeline<HttpResponseMessage> Weather { get; init; }
    public required ResiliencePipeline<HttpResponseMessage> Traffic { get; init; }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.