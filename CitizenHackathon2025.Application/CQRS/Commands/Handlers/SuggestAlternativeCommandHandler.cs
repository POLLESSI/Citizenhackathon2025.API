using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.DTOs.UI;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Commands.Handlers
{
    public sealed class SuggestAlternativeCommandHandler : IRequestHandler<SuggestAlternativeCommand, SuggestionUIResponseDTO>
    {
        private readonly ICrowdInfoService _crowd;
        private readonly ITrafficConditionService _traffic;
        private readonly IOpenWeatherService _weather;
        private readonly IMistralAIService _mistral;
        private readonly IUiTextLocalizer _texts;

        public SuggestAlternativeCommandHandler(
            ICrowdInfoService crowd,
            ITrafficConditionService traffic,
            IOpenWeatherService weather,
            IMistralAIService mistral,
            IUiTextLocalizer texts)
        {
            _crowd = crowd;
            _traffic = traffic;
            _weather = weather;
            _mistral = mistral;
            _texts = texts;
        }

        public async Task<SuggestionUIResponseDTO> Handle(
            SuggestAlternativeCommand request,
            CancellationToken ct)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var lang = _texts.NormalizeLanguage(request.LanguageCode);

            if (string.IsNullOrWhiteSpace(request.Destination))
            {
                return new SuggestionUIResponseDTO
                {
                    Text = _texts.InvalidDestination(lang),
                    Severity = "Medium",
                    Icon = "❓"
                };
            }

            var crowd = await _crowd.GetCrowdLevelAsync(request.Destination);
            var traffic = await _traffic.CheckRoadAsync(request.UserPosition, request.Destination);
            var weather = await _weather.GetForecastAsync(request.Destination);

            if (weather is null)
            {
                return new SuggestionUIResponseDTO
                {
                    Text = _texts.WeatherUnavailable(lang),
                    Severity = "Medium",
                    Icon = "❓"
                };
            }

            var weatherSummary = weather.Summary ?? string.Empty;
            var trafficDescription = traffic?.Description ?? "traffic unspecified";
            var isBlocked = traffic?.IsBlocked ?? false;
            var isOverloaded = crowd?.IsOverloaded ?? false;

            var isSevereWeather =
                weatherSummary.Contains("rain", StringComparison.OrdinalIgnoreCase) ||
                weatherSummary.Contains("thunderstorm", StringComparison.OrdinalIgnoreCase) ||
                weatherSummary.Contains("orage", StringComparison.OrdinalIgnoreCase) ||
                weatherSummary.Contains("storm", StringComparison.OrdinalIgnoreCase) ||
                weatherSummary.Contains("regen", StringComparison.OrdinalIgnoreCase) ||
                weatherSummary.Contains("onweer", StringComparison.OrdinalIgnoreCase) ||
                weatherSummary.Contains("gewitter", StringComparison.OrdinalIgnoreCase);

            if (isSevereWeather || isBlocked || isOverloaded)
            {
                var crowdLabel = isOverloaded
                    ? "high attendance"
                    : "normal attendance";

                var reason = $"{weatherSummary}, {trafficDescription}, {crowdLabel}";
                var prompt = _texts.BuildAlternativePrompt(lang, request.Destination, reason);

                var alt = await _mistral.GenerateFromPromptAsync(prompt, ct);

                return new SuggestionUIResponseDTO
                {
                    Text = string.IsNullOrWhiteSpace(alt)
                        ? _texts.AlternativeFallback(lang)
                        : alt.Trim(),
                    Severity = "High",
                    Icon = "⚠️"
                };
            }

            return new SuggestionUIResponseDTO
            {
                Text = _texts.Recommended(lang),
                Severity = "Low",
                Icon = "✅"
            };
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.