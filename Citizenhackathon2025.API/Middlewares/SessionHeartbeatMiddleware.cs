using CitizenHackathon2025.Domain.Interfaces;
using System.Security.Claims;

namespace CitizenHackathon2025.API.Middlewares
{
    public class SessionHeartbeatMiddleware
    {
        private const string JtiClaim = "jti"; // ✅ the name in the JWT

        public SessionHeartbeatMiddleware(RequestDelegate next) => _next = next;
        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext ctx, IUserSessionRepository repo)
        {
            var user = ctx.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var jti = user.FindFirstValue(JtiClaim); // ✅ no more dependence
                var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;

                if (!string.IsNullOrEmpty(jti) && !string.IsNullOrEmpty(email))
                {
                    if (await repo.IsRevokedAsync(jti))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await ctx.Response.WriteAsync("Session revoked.");
                        return;
                    }
                    await repo.TouchAsync(jti, DateTime.UtcNow);
                }
            }
            await _next(ctx);
        }
    }
}
























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.