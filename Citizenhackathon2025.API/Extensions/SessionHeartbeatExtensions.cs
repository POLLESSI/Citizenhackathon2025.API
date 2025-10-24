using CitizenHackathon2025.API.Middlewares;

namespace CitizenHackathon2025.API.Extensions
{
    public static class SessionHeartbeatExtensions
    {
        public static IApplicationBuilder UseSessionHeartbeat(this IApplicationBuilder app)
            => app.UseMiddleware<SessionHeartbeatMiddleware>();
    }
}
