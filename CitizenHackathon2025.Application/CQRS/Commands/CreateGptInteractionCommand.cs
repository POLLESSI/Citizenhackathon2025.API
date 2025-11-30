using MediatR;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.CQRS.Commands
{
    // Simple, sufficient for your current handler
    public sealed class CreateGptInteractionCommand : IRequest<GptInteractionDTO>
    {
        public string Prompt { get; }
        public string Response { get; }

        public CreateGptInteractionCommand(string prompt, string response)
        {
            Prompt = prompt ?? string.Empty;
            Response = response ?? string.Empty;
        }
    }
}


