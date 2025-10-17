using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
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
        Task<GPTInteraction?> GetChatGptByIdAsync(int id);
        Task<string> SummarizeTextAsync(string input);
        Task<string> GenerateSuggestionAsync(string prompt);
        Task<string> AskChatGptAsync(string prompt);
        Task<string> TranslateToFrenchAsync(string englishText);
        Task<string> TranslateToDutchAsync(string englishText);
        Task<string> TranslateToGermanAsync(string englishText);
        Task<string> SuggestAlternativeAsync(string prompt);
        Task<string> SuggestAlternativeWithWeatherAsync(string location);
        Task SaveInteractionAsync(string prompt, string reply, DateTime createdAt);
        Task<GPTInteraction?> GetByIdAsync(int id);
    }
}







































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.