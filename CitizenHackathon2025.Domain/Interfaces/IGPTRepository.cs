using System.Threading.Tasks;
using System.Collections.Generic;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.Suggestion;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IGPTRepository
    {
        /// <summary>
        /// Sauvegarde une suggestion générée par GPT dans la base.
        /// </summary>
        /// <param name="suggestion">La suggestion générée par GPT.</param>
        Task SaveSuggestionAsync(Suggestion suggestion);

        /// <summary>
        /// Retourne toutes les suggestions enregistrées en base.
        /// </summary>
        /// <returns>Liste des suggestions GPT.</returns>
        Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync();

        /// <summary>
        /// Retourne les suggestions GPT liées à un événement précis.
        /// </summary>
        /// <param name="id">ID de l'événement.</param>
        Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id);

        /// <summary>
        /// Retourne les suggestions GPT liées à une météo donnée.
        /// </summary>
        /// <param name="id">ID de la prévision météo.</param>
        Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id);

        /// <summary>
        /// Retourne les suggestions GPT liées à une condition de trafic.
        /// </summary>
        /// <param name="id">ID de la condition de trafic.</param>
        Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id);

        /// <summary>
        /// Supprime une suggestion générée par GPT.
        /// </summary>
        /// <param name="id">ID de la suggestion.</param>
        Task DeleteSuggestionAsync(int id); 
        
        Task<string> AskAsync(string question);
    }
}
