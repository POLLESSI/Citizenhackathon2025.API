namespace CitizenHackathon2025.API.Middlewares
{
    public class UserAgentFilteringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserAgentFilteringMiddleware> _logger;

        // Blacklist of known agents for scans or bots
        private static readonly List<string> BlacklistedAgents = new()
        {
            "curl", "httpie", "wget", "python", "nmap", "sqlmap",
            "nikto", "fuzz", "scanner", "libwww", "winhttp", "bot"
        };
        public UserAgentFilteringMiddleware(RequestDelegate next, ILogger<UserAgentFilteringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            if (string.IsNullOrWhiteSpace(userAgent) || IsBlacklisted(userAgent))
            {
                _logger.LogWarning("❌ Request blocked - Suspicious User-Agent : {UserAgent}", userAgent);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden - Invalid User-Agent.");
                return;
            }

            await _next(context);
        }

        private static bool IsBlacklisted(string userAgent)
        {
            var lower = userAgent.ToLowerInvariant();
            return BlacklistedAgents.Any(b => lower.Contains(b));
        }
    }

    public static class UserAgentFilteringExtensions
    {
        public static IApplicationBuilder UseUserAgentFiltering(this IApplicationBuilder app)
        {
            return app.UseMiddleware<UserAgentFilteringMiddleware>();
        }
    }
}

