using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Filters
{
    public class ThrottleHubFilter : IHubFilter
    {
        private static readonly ConcurrentDictionary<string, (int Count, DateTime Window)> Counters = new();

        public async ValueTask<object> InvokeMethodAsync(
            HubInvocationContext ctx,
            Func<HubInvocationContext, ValueTask<object>> next)
        {
            var key = $"{ctx.Context.UserIdentifier}:{ctx.HubMethodName}";
            var now = DateTime.UtcNow;
            var (count, win) = Counters.GetOrAdd(key, _ => (0, now));

            if ((now - win) > TimeSpan.FromSeconds(1)) { Counters[key] = (1, now); }
            else
            {
                if (count >= 10) throw new HubException("Rate limit exceeded.");
                Counters[key] = (count + 1, win);
            }
            return await next(ctx);
        }
    }
}













































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.