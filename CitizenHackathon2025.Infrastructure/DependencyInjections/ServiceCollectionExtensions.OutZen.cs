using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CitizenHackathon2025.Infrastructure.DependencyInjections
{
    /// <summary>
    /// Registers “OutZen” services (application layer + infrastructure).
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers weather-related services / OutZen.
        /// Use TryAdd to avoid duplicates if already registered elsewhere.
        /// </summary>
        public static IServiceCollection AddOutZenServices(this IServiceCollection services)
        {
            // Weather application service (CQRS + hub)
            services.TryAddScoped<IWeatherForecastService, WeatherForecastService>();

            // ⬇ If you ever want to centralize other OutZen services here,
            // You can add them using TryAddScoped / TryAddSingleton
            // ex :
            // services.TryAddScoped<IWeatherHubService, WeatherHubService>();
            // services.TryAddScoped<ITrafficConditionService, TrafficConditionService>();
            // etc.

            return services;
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.