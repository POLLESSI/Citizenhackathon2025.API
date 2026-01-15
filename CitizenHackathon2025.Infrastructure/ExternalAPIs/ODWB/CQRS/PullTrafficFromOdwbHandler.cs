using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using MediatR;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.CQRS
{
    public sealed class PullTrafficFromOdwbHandler : IRequestHandler<PullTrafficFromOdwbCommand, int>
    {
        private readonly ITrafficIngestionService _svc;
        public PullTrafficFromOdwbHandler(ITrafficIngestionService svc) => _svc = svc;

        public Task<int> Handle(PullTrafficFromOdwbCommand request, CancellationToken ct)
            => _svc.PullAndUpsertAsync(request.Query, ct);
    }
}
