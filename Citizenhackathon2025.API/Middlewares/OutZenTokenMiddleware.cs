using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            string? eventId = null;

            // 1️⃣ First check in the JWT (Authorization Bearer or JWT cookie)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                eventId = context.User.FindFirst("event_id")?.Value;
            }

            // 2️⃣ Otherwise fallback on access_token in the querystring (useful for SignalR WebSockets)
            if (string.IsNullOrEmpty(eventId))
            {
                var token = context.Request.Query["access_token"].FirstOrDefault();

                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(token);

                        // ⚠️ Here we assume that your JWT contains a claim event_id
                        eventId = jwt.Claims.FirstOrDefault(c => c.Type == "event_id")?.Value;
                    }
                    catch
                    {
                        // Invalid token → rejection
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Invalid or expired OutZen token.");
                        return;
                    }
                }
            }

            // 3️⃣ If still not found → Unauthorized
            if (string.IsNullOrEmpty(eventId))
            {
                // We only block sensitive roads
                var path = context.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/api/suggestion", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/outzenhub", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Missing OutZen eventId.");
                    return;
                }
            }
            else
            {
                // Injection into HttpContext for controllers & hubs
                context.Items["OutZen.EventId"] = eventId;
            }

            await _next(context);
        }
    }
}
