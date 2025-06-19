using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CitizenHackathon2025.API.Middlewares
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;
            var userAgent = request.Headers["User-Agent"].ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var method = request.Method;
            var path = request.Path;

            await _next(context);

            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;

            _logger.LogInformation(
                "📋 [{Time}] {Method} {Path} responded {StatusCode} in {Elapsed}ms | IP: {IP} | Agent: {UserAgent}",
                DateTime.UtcNow,
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                ip,
                userAgent
            );
        }
    }
    public static class AuditLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuditLoggingMiddleware>();
        }
    }
}
