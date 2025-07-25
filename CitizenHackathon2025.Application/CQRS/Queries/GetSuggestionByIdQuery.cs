﻿using MediatR;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.CQRS.Queries
{
    public record GetSuggestionByIdQuery(int Id) : IRequest<Suggestion?>;
}



































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.