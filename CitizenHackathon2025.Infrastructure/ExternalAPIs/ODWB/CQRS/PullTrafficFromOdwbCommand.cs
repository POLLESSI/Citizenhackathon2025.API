using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using MediatR;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.CQRS
{
    public sealed record PullTrafficFromOdwbCommand(OdwbQuery Query) : IRequest<int>;
}
