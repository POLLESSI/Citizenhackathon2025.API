using CitizenHackathon2025.Contracts.Enums;
using Microsoft.AspNetCore.Http;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUserSessionService
    {
        Task TrackAccessTokenAsync(string accessToken, string email, SessionSource source, HttpContext http);
    }
}
