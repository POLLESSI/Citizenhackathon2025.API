using CitizenHackathon2025.DTOs.UI;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Commands
{
    public sealed class SuggestAlternativeCommand : IRequest<SuggestionUIResponseDTO>
    {
        public string Destination { get; }
        public string UserPosition { get; }
        public string LanguageCode { get; }

        public SuggestAlternativeCommand(
            string destination,
            string userPosition,
            string languageCode = "fr")
        {
            Destination = destination ?? string.Empty;
            UserPosition = userPosition ?? string.Empty;
            LanguageCode = string.IsNullOrWhiteSpace(languageCode)
                ? "fr"
                : languageCode.Trim().ToLowerInvariant();
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.