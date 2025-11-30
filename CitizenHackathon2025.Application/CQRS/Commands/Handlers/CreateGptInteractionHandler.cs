using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Application.Extensions; // MapToGptInteraction / MapToGptInteractionDTO

namespace CitizenHackathon2025.Application.CQRS.Commands.Handlers
{
    public sealed class CreateGptInteractionHandler
        : IRequestHandler<CreateGptInteractionCommand, GptInteractionDTO>
    {
        private readonly IGptInteractionRepository _gptInteractionRepository;

        public CreateGptInteractionHandler(IGptInteractionRepository gptInteractionRepository)
        {
            _gptInteractionRepository = gptInteractionRepository;
        }

        public async Task<GptInteractionDTO> Handle(
            CreateGptInteractionCommand cmd,
            CancellationToken ct)
        {
            // 1) Minimum DTO
            var dto = new GptInteractionDTO
            {
                Prompt = cmd.Prompt,
                Response = cmd.Response,
                Active = true
            };

            // 2) Map to the entity
            var entity = dto.MapToGptInteraction();

            // 3) Upsert via SP
            var saved = await _gptInteractionRepository.UpsertInteractionAsync(entity);
            if (saved is null)
                throw new InvalidOperationException("Failed to upsert GPTInteraction.");

            // 4) Return DTO
            return saved.MapToGptInteractionDTO();
        }
    }
}



