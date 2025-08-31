using CitizenHackathon2025.Domain.Interfaces;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Commands
{
    public record SoftDeleteSuggestionCommand(int Id) : IRequest<bool>;

    public class SoftDeleteSuggestionHandler : IRequestHandler<SoftDeleteSuggestionCommand, bool>
    {
        private readonly ISuggestionRepository _repository;

        public SoftDeleteSuggestionHandler(ISuggestionRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(SoftDeleteSuggestionCommand request, CancellationToken cancellationToken)
        {
            return await _repository.SoftDeleteSuggestionAsync(request.Id);
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.