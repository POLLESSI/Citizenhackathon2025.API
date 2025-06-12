using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Citizenhackathon2025.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        //public async Task InvokeAsync(HttpContext context)
        //{
        //    var correlationId = Guid.NewGuid().ToString();
        //    context.Items["CorrelationId"] = correlationId;
        //    try
        //    {
        //        // Continue the pipeline
        //        await _next(context);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log and handle any unhandled exception
        //        _logger.LogError(ex, "💥 An unhandled exception was caught !");
        //        await HandleExceptionAsync(context, ex, correlationId);
        //    }
        //}

        //private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        //{
        //    context.Response.ContentType = "application/json";
        //    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        //    var response = new
        //    {
        //        error = "An unexpected error has occurred.",
        //        correlationId,
        //        message = _env.IsDevelopment() ? ex.Message : "An internal error has occurred.",
        //        details = _env.IsDevelopment() ? ex.StackTrace : null
        //    };
        //    return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        //}
    }
    // Extension to integrate into Program.cs
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
