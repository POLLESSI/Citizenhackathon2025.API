using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class UiTextLocalizer : IUiTextLocalizer
    {
        public string NormalizeLanguage(string? languageCode)
        {
            var lang = (languageCode ?? "fr").Trim().ToLowerInvariant();

            return lang switch
            {
                "fr" => "fr",
                "en" => "en",
                "nl" => "nl",
                "de" => "de",
                _ => "fr"
            };
        }

        public string InvalidDestination(string languageCode) => NormalizeLanguage(languageCode) switch
        {
            "en" => "The provided destination is invalid.",
            "nl" => "De opgegeven bestemming is ongeldig.",
            "de" => "Das angegebene Ziel ist ungültig.",
            _ => "La destination fournie est invalide."
        };

        public string WeatherUnavailable(string languageCode) => NormalizeLanguage(languageCode) switch
        {
            "en" => "Unable to retrieve weather data for this destination.",
            "nl" => "Het is niet mogelijk om weergegevens voor deze bestemming op te halen.",
            "de" => "Die Wetterdaten für dieses Ziel konnten nicht abgerufen werden.",
            _ => "Impossible de récupérer les données météo pour cette destination."
        };

        public string Recommended(string languageCode) => NormalizeLanguage(languageCode) switch
        {
            "en" => "The destination is recommended under the current conditions.",
            "nl" => "De bestemming wordt aanbevolen onder de huidige omstandigheden.",
            "de" => "Das Ziel wird onder den aktuellen Bedingungen empfohlen.",
            _ => "La destination est recommandée dans les conditions actuelles."
        };

        public string AlternativeFallback(string languageCode) => NormalizeLanguage(languageCode) switch
        {
            "en" => "An alternative is recommended, but no usable suggestion could be generated.",
            "nl" => "Een alternatief wordt aanbevolen, maar er kon geen bruikbare suggestie worden gegenereerd.",
            "de" => "Eine Alternative wird empfohlen, aber es konnte kein brauchbarer Vorschlag erzeugt werden.",
            _ => "Une alternative est recommandée, mais aucune suggestion exploitable n'a pu être générée."
        };

        public string BuildAlternativePrompt(string languageCode, string destination, string reason)
        {
            return NormalizeLanguage(languageCode) switch
            {
                "en" =>
                    $"Suggest one relevant alternative in Belgium to the destination '{destination}' because of: {reason}. " +
                    $"Answer in English, in one short, clear, concrete and useful sentence. Do not invent unnecessary information.",

                "nl" =>
                    $"Stel één relevant alternatief in België voor voor de bestemming '{destination}' omwille van: {reason}. " +
                    $"Antwoord in het Nederlands, in één korte, duidelijke, concrete en nuttige zin. Verzin geen onnodige informatie.",

                "de" =>
                    $"Schlage genau eine passende Alternative in Belgien für das Ziel '{destination}' vor, wegen: {reason}. " +
                    $"Antworte auf Deutsch, in einem kurzen, klaren, konkreten und nützlichen Satz. Erfinde keine unnötigen Informationen.",

                _ =>
                    $"Propose une seule alternative pertinente en Belgique à la destination '{destination}' à cause de : {reason}. " +
                    $"Réponds en français, en une phrase courte, claire, concrète et utile. N'invente pas d'information inutile."
            };
        }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.