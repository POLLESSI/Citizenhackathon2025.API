using CitizenHackathon2025.Application.DTOs;
using CitizenHackathon2025.Domain.Interfaces; // ITrafficConditionRepository
using CitizenHackathon2025.DTOs.DTOs;
using Mapster;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Queries
{
    /// <summary>
    /// Requests the latest traffic conditions in the form of DTOs.
    /// </summary>
    public sealed record GetLatestTrafficConditionQuery : IRequest<IReadOnlyList<TrafficConditionDTO>>;
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.