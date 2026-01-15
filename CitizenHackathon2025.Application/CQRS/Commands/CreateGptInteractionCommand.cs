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
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.