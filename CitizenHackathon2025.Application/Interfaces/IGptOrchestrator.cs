using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IGptOrchestrator
    {
        Task<GptStartResponseDto> StartMistralRequestAsync(GptPromptRequest request,CancellationToken ct = default);
        Task<GptInteractionDTO> RunMistralRequestAsync(GptPromptRequest request, CancellationToken ct = default);

        Task<bool> CancelAsync(int interactionId, string? requestId = null);
    }
}