using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class GptHubContextExtensions
    {
        public static async Task SendStarted(
            this IHubContext<GPTHub, IGptClient> hubContext,
            GptResponseStartedDto dto,
            ILogger? logger = null)
        {
            logger?.LogInformation(
                "[GPT-HUB] SendStarted -> InteractionId={InteractionId}, RequestId={RequestId}",
                dto.InteractionId,
                dto.RequestId);

            await hubContext.Clients.All.ReceiveGptResponseStarted(dto);
        }

        public static async Task SendChunk(
            this IHubContext<GPTHub, IGptClient> hubContext,
            GptResponseChunkDto dto,
            ILogger? logger = null)
        {
            logger?.LogInformation(
                "[GPT-HUB] SendChunk -> InteractionId={InteractionId}, RequestId={RequestId}, ChunkLength={ChunkLength}, IsFinal={IsFinal}",
                dto.InteractionId,
                dto.RequestId,
                dto.Chunk?.Length ?? 0,
                dto.IsFinal);

            await hubContext.Clients.All.ReceiveGptResponseChunk(dto);
        }

        public static async Task SendStatus(
            this IHubContext<GPTHub, IGptClient> hubContext,
            GptResponseStatusDto dto,
            ILogger? logger = null)
        {
            logger?.LogInformation(
                "[GPT-HUB] SendStatus -> InteractionId={InteractionId}, RequestId={RequestId}, Status={Status}",
                dto.InteractionId,
                dto.RequestId,
                dto.Status);

            await hubContext.Clients.All.ReceiveGptResponseStatus(dto);
        }

        public static async Task SendCompleted(
            this IHubContext<GPTHub, IGptClient> hubContext,
            GptInteractionCompletedDto dto,
            ILogger? logger = null)
        {
            logger?.LogInformation(
                "[GPT-HUB] SendCompleted -> InteractionId={InteractionId}",
                dto.Id);

            await hubContext.Clients.All.ReceiveGptResponseCompleted(dto);
        }
    }
}


























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.