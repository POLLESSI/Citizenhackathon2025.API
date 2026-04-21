using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Application.Services
{
    public sealed class MistralContextBuilder
    {
        private readonly ILocalAiContextService _localAiContextService;

        public MistralContextBuilder(ILocalAiContextService localAiContextService)
        {
            _localAiContextService = localAiContextService
                ?? throw new ArgumentNullException(nameof(localAiContextService));
        }

        public async Task<string> BuildContextAsync(
            string userPrompt,
            double? latitude = null,
            double? longitude = null,
            CancellationToken ct = default)
        {
            var context = await _localAiContextService.BuildContextAsync(
                userPrompt,
                latitude,
                longitude,
                ct);

            return _localAiContextService.BuildPrompt(context);
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.