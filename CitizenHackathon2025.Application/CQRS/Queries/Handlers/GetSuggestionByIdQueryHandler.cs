using MediatR;
using Citizenhackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.CQRS.Queries.Handlers
{
    public class GetSuggestionByIdQueryHandler : IRequestHandler<GetSuggestionByIdQuery, Suggestion?>
    {
        private readonly ISuggestionRepository _repository;

        public GetSuggestionByIdQueryHandler(ISuggestionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Suggestion?> Handle(GetSuggestionByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
