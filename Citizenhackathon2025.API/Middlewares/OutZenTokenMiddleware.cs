using CitizenHackathon2025.API.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CitizenHackathon2025.API.Middlewares
{
    public class OutZenTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OutZenTokenMiddleware> _logger;
        private readonly bool _allowHeaderFallback;
        private readonly JwtOptions _jwt;

        public OutZenTokenMiddleware(
            RequestDelegate next,
            ILogger<OutZenTokenMiddleware> logger,
            IConfiguration configuration,
            IOptions<JwtOptions> jwtOptions)
        {
            _next = next;
            _logger = logger;
            _jwt = jwtOptions.Value;

            _allowHeaderFallback = configuration.GetValue<bool>("OutZen:AllowHeaderFallback");
        }

        public async Task Invoke(HttpContext context)
        {
            string? eventId = null;
            string source = "None";

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                eventId = context.User.FindFirst("event_id")?.Value;
                if (!string.IsNullOrEmpty(eventId))
                    source = "JWT";
            }

            if (string.IsNullOrEmpty(eventId))
            {
                var token = context.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(_jwt.Secret))
                            throw new InvalidOperationException("JWT Secret is not configured.");

                        var handler = new JwtSecurityTokenHandler();
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
                        var parameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = key,
                            ValidateIssuer = !string.IsNullOrWhiteSpace(_jwt.Issuer),
                            ValidIssuer = _jwt.Issuer,
                            ValidateAudience = !string.IsNullOrWhiteSpace(_jwt.Audience),
                            ValidAudience = _jwt.Audience,
                            RequireExpirationTime = true,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromMinutes(2)
                        };

                        var principal = handler.ValidateToken(token, parameters, out _);
                        eventId = principal.FindFirst("event_id")?.Value;
                        if (!string.IsNullOrEmpty(eventId))
                            source = "QueryString";
                    }
                    catch (SecurityTokenExpiredException ex)
                    {
                        _logger.LogWarning(ex, "Expired OutZen token in query string.");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Expired OutZen token.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Invalid OutZen token in query string.");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Invalid OutZen token.");
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(eventId) && _allowHeaderFallback)
            {
                if (context.Request.Headers.TryGetValue("X-OutZen-EventId", out var headerEventId))
                {
                    eventId = headerEventId.ToString();
                    if (!string.IsNullOrEmpty(eventId))
                        source = "Header";
                }
            }

            if (string.IsNullOrEmpty(eventId))
            {
                var path = context.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/api/suggestion", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/outzenhub", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Request blocked: Missing OutZen eventId. Path={Path}", path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Missing OutZen eventId.");
                    return;
                }

                _logger.LogDebug("No eventId provided. Request allowed for path {Path}", path);
            }
            else
            {
                context.Items["OutZen.EventId"] = eventId;
                _logger.LogInformation("OutZen EventId resolved: {EventId} (source: {Source})", eventId, source);
            }

            await _next(context);
        }
    }
}