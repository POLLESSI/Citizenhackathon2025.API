using CitizenHackathon2025.API.Security;
using Serilog;

namespace CitizenHackathon2025.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var bodyString = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            Log.ForContext(new ScrubEnricher("Body", bodyString))
               .Information("HTTP {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context);
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.