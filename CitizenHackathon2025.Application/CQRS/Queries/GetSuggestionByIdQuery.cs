using MediatR;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.CQRS.Queries
{
    public record GetSuggestionByIdQuery(int Id) : IRequest<Suggestion?>;
}
