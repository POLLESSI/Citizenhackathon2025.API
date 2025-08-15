using CitizenHackathon2025.API.Security;

namespace CitizenHackathon2025.API.Middlewares
{
    public class OutZenTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public OutZenTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
        #nullable disable
            var path = context.Request.Path.Value;
            if (path != null && path.StartsWith("/api/outzen", StringComparison.OrdinalIgnoreCase))
            {
                var token = context.Request.Query["access_token"].FirstOrDefault();
                if (!OutZenAccessTokenValidator.Validate(token, out var eventId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid or expired OutZen token.");
                    return;
                }

                // Saves the eventId in the context for later injection
                context.Items["OutZen.EventId"] = eventId;
            }

            await _next(context);
        }
    }
}
