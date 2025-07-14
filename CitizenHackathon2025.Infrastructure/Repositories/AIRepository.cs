using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    #nullable disable
    public class AIRepository : IAIRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<AIRepository> _logger;

        public AIRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task SaveInteractionAsync(GPTInteraction interaction)
        {
            
            try
            {
                var sql = @"INSERT INTO GPTInteractions (Prompt, Response, CreatedAt, Active)
                            VALUES (@Prompt, @Response, @CreatedAt, @Active);";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Prompt", interaction.Prompt);
                parameters.Add("Response", interaction.Response);
                parameters.Add("CreatedAt", interaction.CreatedAt);
                parameters.Add("Active", interaction.Active);

                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error saving GPT interaction.");
                throw;
            }
        }

        public async Task<GPTInteraction?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = @"SELECT * FROM GPTInteractions WHERE Id = @Id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Id", id);

                return await _connection.QueryFirstOrDefaultAsync<GPTInteraction>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving GPT interaction.");
                throw;
            }
            
        }

        public async Task<string> GetSuggestionsAsync(object content)
        {
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true });
            return await Task.FromResult($"Suggestion (mock) based on:\n{jsonContent}");
        }

        public async Task<string> GetTouristicSuggestionsAsync(string prompt)
        {
            return await Task.FromResult($"Touristic suggestion (mock): {prompt}");
        }

        public async Task<GPTInteraction?> GetChatGptByIdAsync(int id)
        {
            return await GetByIdAsync(id); 
        }


        public async Task<string> SummarizeTextAsync(string input)
        {
            return await Task.FromResult($"Summary (mock): {input[..Math.Min(input.Length, 80)]}...");
        }

        public async Task<string> GenerateSuggestionAsync(string prompt)
        {
            return await Task.FromResult($"Generated suggestion (mock) for : {prompt}");
        }

        public async Task<string> AskChatGptAsync(string prompt)
        {
            return await Task.FromResult($"GPT (mock) response to : {prompt}");
        }

        public async Task<string> TranslateToFrenchAsync(string englishText)
        {
            return await Task.FromResult($"[FR] Traduction (mock) : {englishText}");
        }

        public async Task<string> TranslateToDutchAsync(string englishText)
        {
            return await Task.FromResult($"[NL] Vertaling (mock): {englishText}");
        }

        public async Task<string> TranslateToGermanAsync(string englishText)
        {
            return await Task.FromResult($"[DE] Übersetzung (mock): {englishText}");
        }

        public async Task<string> SuggestAlternativeAsync(string prompt)
        {
            return await Task.FromResult($"Suggested alternative (mock) for : {prompt}");
        }

        public async Task<string> SuggestAlternativeWithWeatherAsync(string location)
        {
            return await Task.FromResult($"Alternative (mock) for {location} with unknown weather.");
        }
    }
}
