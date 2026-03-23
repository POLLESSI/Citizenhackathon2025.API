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
                ctx.Response.OnStarting(() =>
                {
                    var h = ctx.Response.Headers;
                    var env = ctx.RequestServices.GetService<IWebHostEnvironment>();
                    var isDev = env?.IsDevelopment() == true;
                    var isSwagger = ctx.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);

                    h["X-Content-Type-Options"] = "nosniff";
                    h["X-Frame-Options"] = "DENY";
                    h.Remove("X-XSS-Protection");
                    h["Referrer-Policy"] = "no-referrer";
                    h["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), fullscreen=(self)";

                    if (isSwagger && isDev)
                    {
                        h["Content-Security-Policy"] =
                            "default-src 'self'; " +
                            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                            "style-src 'self' 'unsafe-inline'; " +
                            "img-src 'self' data:; " +
                            "font-src 'self' data:; " +
                            "connect-src 'self' https://localhost:7254 wss://localhost:7254 http://localhost:* ws://localhost:* wss://localhost:*; " +
                            "frame-ancestors 'none'; " +
                            "base-uri 'self'; " +
                            "form-action 'self';";
                    }
                    else
                    {
                        h["Content-Security-Policy"] =
                            "default-src 'self'; " +
                            "connect-src 'self' https://localhost:7254 wss://localhost:7254; " +
                            "script-src 'self'; " +
                            "style-src 'self' 'unsafe-inline'; " +
                            "img-src 'self' data:; " +
                            "font-src 'self' data:; " +
                            "frame-ancestors 'none'; " +
                            "base-uri 'self'; " +
                            "form-action 'self';";
                    }

                    return Task.CompletedTask;
                });

                await next();
            });
        }
    }
}



































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.