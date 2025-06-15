using Microsoft.Extensions.Caching.Memory;

namespace CitizenHackathon2025.Application.Services
{
    public class MemoryCacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T? GetOrAdd<T>(string key, Func<T> factory, TimeSpan duration)
        {
            if (_cache.TryGetValue(key, out T? value))
                return value;

            value = factory();
            _cache.Set(key, value, duration);
            return value;
        }

        public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T?>> factory, TimeSpan duration)
        {
            if (_cache.TryGetValue(key, out T? value))
                return value;

            value = await factory();
            if (value != null)
                _cache.Set(key, value, duration);

            return value;
        }
    }
}
