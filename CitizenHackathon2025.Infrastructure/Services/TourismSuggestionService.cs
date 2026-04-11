using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Application.Services
{
    public sealed class TourismSuggestionService
    {
        private readonly IGenerativeAiService _ai;
        private readonly ILogger<TourismSuggestionService> _logger;

        public TourismSuggestionService(
            IGenerativeAiService ai,
            ILogger<TourismSuggestionService> logger)
        {
            _ai = ai;
            _logger = logger;
        }

        public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
        {
            var finalPrompt =
                """
                You are a reliable local tourist assistant.
                You must not invent facts that are absent from the context.
                Provide a useful, concise, and actionable answer.
                """ + "\n\n" + prompt;

            return await _ai.GenerateTextAsync(finalPrompt, ct);
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.