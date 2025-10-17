// CitizenHackathon2025.Shared/Observability/ObservabilityContext.cs
using System.Diagnostics;
using Polly;

namespace CitizenHackathon2025.Shared.Observability
{
    public static class ObservabilityContext
    {
        private static readonly ResiliencePropertyKey<string> ServiceKey = new("service");
        private static readonly ResiliencePropertyKey<string> OperationKey = new("operation");
        private static readonly ResiliencePropertyKey<string> UserIdKey = new("userId");
        private static readonly ResiliencePropertyKey<string> EventIdKey = new("eventId");
        private static readonly ResiliencePropertyKey<string> CorrelationIdKey = new("correlationId");

        public static (ResilienceContext Ctx, string CorrelationId) Create(
            string service, string operation, string? userId = null, string? eventId = null)
        {
            var corrId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            var ctx = ResilienceContextPool.Shared.Get();

            ctx.Properties.Set(ServiceKey, service);
            ctx.Properties.Set(OperationKey, operation);
            ctx.Properties.Set(UserIdKey, userId ?? "anon");
            ctx.Properties.Set(EventIdKey, eventId ?? "");
            ctx.Properties.Set(CorrelationIdKey, corrId);

            return (ctx, corrId);
        }
    }

    // simple option without compilation directives
    public static class ObservabilityContextV7
    {
        public static (Context Ctx, string CorrelationId) Create(
            string service, string operation, string? userId = null, string? eventId = null)
        {
            var corrId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            var ctx = new Context();
            ctx["service"] = service;
            ctx["operation"] = operation;
            ctx["userId"] = userId ?? "anon";
            ctx["eventId"] = eventId ?? "";
            ctx["correlationId"] = corrId;
            return (ctx, corrId);
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.