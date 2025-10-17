using MediatR;
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



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.