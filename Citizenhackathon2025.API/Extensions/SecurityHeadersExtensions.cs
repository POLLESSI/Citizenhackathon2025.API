using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CitizenHackathon2025.API.Extensions
{
    public static class SecurityHeadersExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (ctx, next) =>
            {
                var h = ctx.Response.Headers;
                h.TryAdd("X-Content-Type-Options", "nosniff");
                h.TryAdd("X-Frame-Options", "DENY");
                h.Remove("X-XSS-Protection"); // obsolete

                h.TryAdd("Referrer-Policy", "no-referrer");
                h.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=(), fullscreen=(self)");

                // ⚠️ Adjust connect-src (API + hubs) by approx
                h.TryAdd("Content-Security-Policy",
                    "default-src 'self'; " +
                    "connect-src 'self' https://localhost:7254 wss://localhost:7254; " +
                    "script-src 'self'; style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; frame-ancestors 'none'");

                await next();
            });
        }
    }
}




























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.