using CitizenHackathon2025.Contracts.Enums;
using Microsoft.AspNetCore.Http;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUserSessionService
    {
        Task TrackAccessTokenAsync(string accessToken, string email, SessionSource source, HttpContext http);
        Task TrackLoginAsync(string email, string jti, DateTime issuedAtUtc, DateTime expiresAtUtc, string? ip, string? userAgent, CancellationToken ct = default);
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.