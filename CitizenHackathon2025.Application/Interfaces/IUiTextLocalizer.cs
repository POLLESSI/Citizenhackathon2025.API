namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUiTextLocalizer
    {
        string NormalizeLanguage(string? languageCode);
        string InvalidDestination(string languageCode);
        string WeatherUnavailable(string languageCode);
        string Recommended(string languageCode);
        string AlternativeFallback(string languageCode);
        string BuildAlternativePrompt(string languageCode, string destination, string reason);
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.