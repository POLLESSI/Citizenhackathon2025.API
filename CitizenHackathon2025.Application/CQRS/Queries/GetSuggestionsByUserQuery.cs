using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Queries
{
    public record GetSuggestionsByUserQuery(int UserId) : IRequest<IEnumerable<Suggestion>>;

    public class GetSuggestionsByUserHandler : IRequestHandler<GetSuggestionsByUserQuery, IEnumerable<Suggestion>>
    {
        private readonly ISuggestionRepository _repository;

        public GetSuggestionsByUserHandler(ISuggestionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Suggestion>> Handle(GetSuggestionsByUserQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetSuggestionsByUserAsync(request.UserId);
        }
    }
}




































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.