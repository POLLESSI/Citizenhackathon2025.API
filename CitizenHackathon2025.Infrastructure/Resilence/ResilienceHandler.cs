using Polly;

namespace CitizenHackathon2025.Infrastructure.Resilience
{
    public class ResilienceHandler : DelegatingHandler
    {
        private readonly AsyncPolicy<HttpResponseMessage> _policy;

        public ResilienceHandler(AsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Use an empty Context and pass the CancellationToken separately
            return await _policy.ExecuteAsync(
                async (ctx, ct) => await base.SendAsync(request, ct),
                new Context(),  // ✅ Empty context
                cancellationToken);
        }
    }
}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.