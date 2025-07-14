using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
using CitizenHackathon2025.Domain.ValueObjects;
using CitizenHackathon2025.DTOs.UI;

namespace CitizenHackathon2025.Application.CQRS.Commands
{
    public class SuggestAlternativeCommand : IRequest<SuggestionUIResponseDTO>
    {
        public string Destination { get; set; }
        public string UserPosition { get; set; } 

        public SuggestAlternativeCommand(string destination, string userPosition)
        {
            Destination = destination;
            UserPosition = userPosition;
        }
    }
}
