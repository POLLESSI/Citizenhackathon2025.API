using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.API.Middlewares
{
    public class RefreshTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public RefreshTokenValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRefreshTokenRepository refreshTokenRepo)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // We retrieve the "refreshToken" claim added in your JWT
                var refreshTokenValue = context.User.FindFirst("refreshToken")?.Value;

                if (!string.IsNullOrEmpty(refreshTokenValue))
                {
                    var refreshToken = await refreshTokenRepo.GetByTokenAsync(refreshTokenValue);

                    // Business rule: a refresh token is valid ONLY if it is active
                    if (refreshToken == null || !refreshToken.IsActive())
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Unauthorized: refresh token is revoked or expired.");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class RefreshTokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseRefreshTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RefreshTokenValidationMiddleware>();
        }
    }
}

