namespace CitizenHackathon2025.API.Extensions
{
    public static class SecurityHeadersExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
                    context.Response.Headers.Add("X-Frame-Options", "DENY");

                if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
                    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

                await next();
            });
        }
    }
}
