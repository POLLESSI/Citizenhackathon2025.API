using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitizenHackathon2025.Application.Extensions;     // MapToTrafficConditionDTO
using CitizenHackathon2025.Application.Interfaces;     // ITrafficConditionService
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
// Explicit alias to the correct query
using Query = CitizenHackathon2025.Application.CQRS.Queries.GetLatestTrafficConditionQuery;

namespace CitizenHackathon2025.Application.CQRS.Queries.Handlers
{
    public sealed class GetLatestTrafficConditionQueryHandler
        : IRequestHandler<Query, IReadOnlyList<TrafficConditionDTO>>
    {
        private readonly ITrafficConditionService _service;

        public GetLatestTrafficConditionQueryHandler(ITrafficConditionService service)
            => _service = service;

        public async Task<IReadOnlyList<TrafficConditionDTO>> Handle(
    Query request,
    CancellationToken cancellationToken)
        {
            var items = await _service.GetLatestTrafficConditionAsync(
                limit: 10,                      // ✅ passe le 1er param obligatoire
                ct: cancellationToken);

            if (items is null) return [];

            return items
                .Where(e => e is not null && e!.Active)
                .Select(e => e!.MapToTrafficConditionDTO())
                .OrderByDescending(d => d.DateCondition)
                .ToList();
        }
    }
}
