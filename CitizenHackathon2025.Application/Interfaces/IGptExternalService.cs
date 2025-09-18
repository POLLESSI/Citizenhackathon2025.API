namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IGptExternalService
    {
        Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
        Task<string> RefineSuggestionAsync(string raw, CancellationToken ct = default);
    }
}