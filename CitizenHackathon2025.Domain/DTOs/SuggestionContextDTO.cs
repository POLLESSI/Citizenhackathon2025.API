using CitizenHackathon2025.Domain.Enums;
using System.Text;

namespace CitizenHackathon2025.Domain.DTOs
{
    /// <summary>
    /// Contains the context to generate an OutZen smart suggestion.
    /// Passed to AstroIA and GPT-4 to produce a suggestion.
    /// </summary>
    public class SuggestionContextDTO
    {
        /// <summary>
        /// Current weather status ("sunny", "rainy", "cloudy", etc.).
        /// </summary>
        public string Weather { get; set; } = default!;

        /// <summary>
        /// Estimated traffic level ("light", "moderate", "heavy").
        /// </summary>
        public string Traffic { get; set; } = default!;

        /// <summary>
        /// Level of human presence (“light”, “moderate”, “high”).
        /// </summary>
        public string Crowd { get; set; } = default!;

        /// <summary>
        /// Time of day (dawn, day, sunset, night).
        /// </summary>
        public MomentOfDay Moment { get; set; }

        /// <summary>
        /// Optional: Name of the place or region concerned (e.g.: "Namur", "Huy").
        /// </summary>
        public string? PlaceName { get; set; }

        /// <summary>
        /// Optional: OutZen theme (e.g.: “forest”, “relaxation”, “observation”, etc.).
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// Optional: User context (age, profile, etc.).
        /// </summary>
        public string? UserProfile { get; set; }

        /// <summary>
        /// Optional: Preferred language for response (e.g. "fr", "en", "nl").
        /// </summary>
        public string? Language { get; set; } = "fr";
        /// <summary>
        /// Generates a text prompt to GPT based on context.
        /// </summary>
        public string ToPromptString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a smart assistant in a Belgian outing suggestion app called OutZen.");
            sb.AppendLine($"The current context is as follows :");

            if (!string.IsNullOrWhiteSpace(PlaceName))
                sb.AppendLine($"- Place : {PlaceName}");

            sb.AppendLine($"- Weather report : {Weather}");
            sb.AppendLine($"- Traffic : {Traffic}");
            sb.AppendLine($"- Crowd : {Crowd}");
            sb.AppendLine($"- Time of day : {Moment}");

            if (!string.IsNullOrWhiteSpace(Theme))
                sb.AppendLine($"- Theme sought : {Theme}");

            if (!string.IsNullOrWhiteSpace(UserProfile))
                sb.AppendLine($"- User profile : {UserProfile}");

            sb.AppendLine();
            sb.AppendLine("Give a suitable, original and kind suggestion to do now in this region.");
            sb.AppendLine("The suggestion should be concise, inspiring, local and not involve long travel.");

            if (Language?.ToLower() == "en")
                sb.AppendLine("Please answer in English.");
            else if (Language?.ToLower() == "nl")
                sb.AppendLine("Beantwoord dit in het Nederlands.");
            else
                sb.AppendLine("Réponds en français.");

            return sb.ToString();
        }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.