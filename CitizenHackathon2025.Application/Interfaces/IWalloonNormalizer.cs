using CitizenHackathon2025.Domain.Language;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IWalloonNormalizer
    {
        string Normalize(string input);

        LanguageConfidenceResult EstimateConfidence(string input);
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.