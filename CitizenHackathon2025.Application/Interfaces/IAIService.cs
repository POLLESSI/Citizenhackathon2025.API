using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.GPTInteraction;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IAIService
    {
        Task<string> GetSuggestionsAsync(object content);

        /// <summary>
        /// Sends a structured prompt to the AI ​​(OpenAI) and returns the generated response.
        /// </summary>
        /// <param name="prompt">The structured prompt containing the tourist constraints.</param>
        /// <returns>AI-generated text response.</returns>
        Task<string> GetTouristicSuggestionsAsync(string prompt);
        Task<string> SummarizeTextAsync(string input);
        Task<string> GenerateSuggestionAsync(string prompt);
        Task<string> TranslateToFrenchAsync(string englishText);
        Task<string> TranslateToDutchAsync(string englishText);
        Task<string> TranslateToGermanAsync(string englishText);
        Task<string> SuggestAlternativeAsync(string prompt);
        Task<string> SuggestAlternativeWithWeatherAsync(string location);

    }
}
