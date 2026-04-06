using Polly;

namespace CitizenHackathon2025.Infrastructure.Resilience
{
    public class ResiliencePipelines
    {
    #nullable disable
        public AsyncPolicy<HttpResponseMessage> OpenAi { get; set; }
        public AsyncPolicy<HttpResponseMessage> Traffic { get; set; }
        public AsyncPolicy<HttpResponseMessage> Weather { get; set; }
        public AsyncPolicy<HttpResponseMessage> Ollama { get; set; }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.