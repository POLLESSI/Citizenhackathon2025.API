using MediatR;

namespace CitizenHackathon2025.Application.Suggestions.Commands
{
    public record GenerateSmartSuggestionCommand(SuggestionContextDTO Context) : IRequest<string>;
}
