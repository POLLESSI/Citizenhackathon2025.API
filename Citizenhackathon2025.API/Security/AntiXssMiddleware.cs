namespace CitizenHackathon2025.API.Security
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
            // Adds anti-XSS headers
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(self), microphone=(), camera=()";

            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "connect-src 'self' https://localhost:7254 https://localhost:7260 wss://localhost:7254 wss://localhost:7051; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';" +
                "report-uri /csp-report;";

            // Cleaning cookies (eg: HttpOnly, Secure, etc.)
            foreach (var cookie in context.Request.Cookies)
            {
                // Here we could disable certain cookies or filter according to logic.
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

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.