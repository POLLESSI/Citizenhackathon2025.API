using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Citizenhackathon2025.API.Security
{
    public class AntiXssMiddleware
    {
        private readonly RequestDelegate _next;

        public AntiXssMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Ajoute des headers anti-XSS
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'";

            // Nettoyage des cookies (ex: HttpOnly, Secure, etc.)
            foreach (var cookie in context.Request.Cookies)
            {
                // On pourrait ici invalider certains cookies ou filtrer selon logique
                if (cookie.Key.StartsWith("X-Tracking"))
                {
                    context.Response.Cookies.Delete(cookie.Key);
                }
            }

            await _next(context);
        }
    }
    // Extension for the pipeline
    public static class AntiXssMiddlewareExtensions
    {
        public static IApplicationBuilder UseAntiXssMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AntiXssMiddleware>();
        }
    }
}
