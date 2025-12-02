using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class UserSessionService : IUserSessionService
    {
        private readonly IUserSessionRepository _repo;
        private readonly ILogger<UserSessionService> _log;
        public UserSessionService(IUserSessionRepository repo, ILogger<UserSessionService> log)
        { _repo = repo; _log = log; }

        public async Task TrackAccessTokenAsync(string accessToken, string email, SessionSource source, HttpContext http)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var ua = http.Request.Headers.UserAgent.ToString();
            if (ua?.Length > 256) ua = ua[..256]; // sanitize
            var ip = http.Connection.RemoteIpAddress?.ToString();
            if (ip?.Length > 64) ip = ip[..64];

            await _repo.UpsertAsync(new UserSession
            {
                UserEmail = email,
                Jti = jwt.Id,
                IssuedAtUtc = jwt.ValidFrom.ToUniversalTime(),
                ExpiresAtUtc = jwt.ValidTo.ToUniversalTime(),
                LastSeenUtc = DateTime.UtcNow,
                //Source = source,
                UserAgent = ua,
                Ip = ip
            });
        }
    }
}





























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.