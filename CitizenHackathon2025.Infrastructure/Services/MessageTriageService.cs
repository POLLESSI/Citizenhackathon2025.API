using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using System.Globalization;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class MessageTriageService : IMessageTriageService
    {
        public Task<MessageTriageResult> AnalyzeAsync(UserMessage message, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var text = Normalize(message.Content);

            if (ContainsAny(
                 text,

                 // "This place is permanently closed."
                 "ferme definitivement",
                 "fermee definitivement",

                 // "The museum is permanently closed."
                 "definitivement ferme",
                 "definitivement fermee",

                 // Permanent closure
                 "ferme pour toujours",
                 "fermee pour toujours",

                 "fermeture definitive",
                 "fermeture permanente",

                 // "He has closed."
                 "a ferme",

                 // "The park has been closed for a few years."
                 "est ferme depuis",
                 "est fermee depuis",

                 // No longer open
                 "n est plus ouvert",
                 "n est plus ouverte",

                 "plus ouvert",
                 "plus ouverte"))
            {
                return Result(AdminMessageCategory.ClosedPlace, priority: 2, confidence: 0.95);
            }

            if (ContainsAny(text, "n existe plus", "existe plus", "a disparu", "a ete demoli", "a ete supprime"))
            {
                return Result(AdminMessageCategory.PlaceNoLongerExists, priority: 2, confidence: 0.95);
            }

            if (ContainsAny(text, "mauvaise adresse", "adresse incorrecte", "mauvais endroit", "mauvaise position", "mal place", "mauvaise localisation", "mauvais emplacement"))
            {
                return Result(AdminMessageCategory.IncorrectLocation, priority: 2, confidence: 0.90);
            }

            if (ContainsAny(text, "horaire faux", "horaires faux", "mauvais horaires", "horaire incorrect", "horaires incorrects"))
            {
                return Result(AdminMessageCategory.WrongOpeningHours, priority: 1, confidence: 0.90);
            }

            if (ContainsAny(text, "evenement annule", "evenement est annule", "concert annule", "festival annule"))
            {
                return Result(AdminMessageCategory.EventCancelled, priority: 2, confidence: 0.95);
            }

            if (ContainsAny(text, "erreur 500", "erreur 404", "exception", "ne fonctionne pas", "fonctionne plus", "bug", "plantage", "crash"))
            {
                return Result(AdminMessageCategory.TechnicalBug, priority: 2, confidence: 0.90);
            }

            if (ContainsAny(text, "lien casse", "lien mort", "page introuvable", "site inaccessible"))
            {
                return Result(AdminMessageCategory.BrokenLink, priority: 1, confidence: 0.90);
            }

            return Task.FromResult(
                new MessageTriageResult
                {
                    RequiresAdminReview = false,
                    Category = AdminMessageCategory.Unknown,
                    Priority = 0,
                    Confidence = 0,
                    ClassificationSource = "Rules"
                });
        }

        private static Task<MessageTriageResult> Result(AdminMessageCategory category, byte priority, double confidence)
        {
            return Task.FromResult(
                new MessageTriageResult
                {
                    RequiresAdminReview = true,
                    Category = category,
                    Priority = priority,
                    Confidence = confidence,
                    ClassificationSource = "Rules"
                });
        }

        private static bool ContainsAny(string text, params string[] expressions)
        {
            return expressions.Any(expression =>
                text.Contains(Normalize(expression), StringComparison.OrdinalIgnoreCase));
        }

        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var decomposed = text.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }

            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.