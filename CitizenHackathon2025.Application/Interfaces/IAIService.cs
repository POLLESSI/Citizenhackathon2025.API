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
        /// Envoie un prompt structuré à l'IA (OpenAI) et retourne la réponse générée.
        /// </summary>
        /// <param name="prompt">Le prompt structuré contenant les contraintes touristiques.</param>
        /// <returns>Réponse textuelle générée par l'IA.</returns>
        Task<string> GetTouristicSuggestionsAsync(string prompt);
        Task<string> SummarizeTextAsync(string input);
        Task<string> TranslateToFrenchAsync(string englishText);
        Task<string> TranslateToDutchAsync(string englishText);
        Task<string> TranslateToGermanAsync(string englishText);

    }
}
