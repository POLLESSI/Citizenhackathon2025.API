using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IMistralAIService
    {
        Task<IEnumerable<Suggestion>> GetWeatherAdvisoryAsync(string location, CancellationToken ct = default);
        Task<string> CallOllamaApi(string prompt, CancellationToken ct);
        Task<int> ArchivePastGptInteractionsAsync();
        Task<string> GenerateFromPromptAsync(string groundedPrompt, CancellationToken ct = default);

        Task<string> StreamFromPromptAsync(string groundedPrompt, Func<GptResponseChunkDto, Task> onChunk, CancellationToken ct = default);
    }
}











































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.