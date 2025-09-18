using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Application.Suggestions.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Application.Suggestions.Handlers
{
    public class GenerateSmartSuggestionHandler : IRequestHandler<GenerateSmartSuggestionCommand, string>
    {
        private readonly AstroIAService _astro;
        private readonly IGptExternalService _gpt;
        private readonly ILogger<GenerateSmartSuggestionHandler> _log;

        public GenerateSmartSuggestionHandler(AstroIAService astro, IGptExternalService gpt, ILogger<GenerateSmartSuggestionHandler> log)
            => (_astro, _gpt, _log) = (astro, gpt, log);

        public async Task<string> Handle(GenerateSmartSuggestionCommand request, CancellationToken cancellationToken)
        {
            var baseSuggestion = await _astro.GenerateSuggestionAsync(request.Context);

            try
            {
                var refined = await _gpt.RefineSuggestionAsync(baseSuggestion, cancellationToken);
                if (!string.IsNullOrWhiteSpace(refined))
                    return refined;
            }
            catch (Exception ex)
            {
                _log.LogWarning("External GPT unavailable, fallback AstroIA: {Message}", ex.Message);
            }

            return baseSuggestion;
        }
    }
}







































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.