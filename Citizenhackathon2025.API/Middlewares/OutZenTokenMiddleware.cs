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
        private readonly bool _requireEventId;
        private readonly JwtOptions _jwt;

        [ActivatorUtilitiesConstructor]
        public OutZenTokenMiddleware(RequestDelegate next, ILogger<OutZenTokenMiddleware> logger, IConfiguration configuration, IOptions<JwtOptions> jwtOptions)
        {
            _next = next;
            _logger = logger;
            _allowHeaderFallback = configuration.GetValue<bool>("OutZen:AllowHeaderFallback");
            _requireEventId      = configuration.GetValue<bool>("OutZen:RequireEventId", true);
            _jwt = jwtOptions.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // ✔️ Allowlist: never eventId required
            if (HttpMethods.IsOptions(context.Request.Method) ||
                path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/User/login", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/User/register", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/Suggestions", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            int? evId = null;
            string source = "None";

            // 1) Claim
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var claimValue = context.User.FindFirst("event_id")?.Value;
                if (int.TryParse(claimValue, out var claimEvId))
                {
                    evId = claimEvId;
                    source = "JWT";
                }
            }

            // 2) Token in query (?access_token=)
            if (evId is null)
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
                        var claimValue = principal.FindFirst("event_id")?.Value;
                        if (int.TryParse(claimValue, out var tokenEvId))
                        {
                            evId = tokenEvId;
                            source = "QueryStringToken";
                        }
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

            // 3) Header (fallback)
            if (evId is null && _allowHeaderFallback &&
                context.Request.Headers.TryGetValue("X-OutZen-EventId", out var hdr) &&
                int.TryParse(hdr.ToString(), out var hdrEvId))
            {
                evId = hdrEvId;
                source = "Header";
            }

            // 4) Simple query param
            if (evId is null)
            {
                var candidate = context.Request.Query["eventId"].FirstOrDefault()
                              ?? context.Request.Headers["X-OutZen-EventId"].FirstOrDefault();
                if (int.TryParse(candidate, out var qEvId))
                {
                    evId = qEvId;
                    source = "QueryParam/Header";
                }
            }

            // 5) If we require an eventId and we don't have one → 401
            if (_requireEventId && evId is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing OutZen eventId.");
                return;
            }

            // 6) OK → store (if present) and continue
            if (evId is not null)
            {
                context.Items["OutZen.EventId"] = evId.Value;
                _logger.LogInformation("OutZen EventId resolved: {EventId} (source: {Source})", evId, source);
            }

            await _next(context);
        }
    }
}