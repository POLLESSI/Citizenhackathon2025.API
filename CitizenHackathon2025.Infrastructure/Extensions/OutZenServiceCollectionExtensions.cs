using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CitizenHackathon2025.Infrastructure.Extensions
{
    /// <summary>
    /// DI extensions for OutZen services (weather, etc.).
    /// </summary>
    public static class OutZenServiceCollectionExtensions
    {
        public static IServiceCollection AddOutZenServices(this IServiceCollection services)
        {
            // TryAdd to avoid duplicates with other possible registers
            services.TryAddScoped<IWeatherForecastService, WeatherForecastService>();

            // You can add other OutZen services here later.
            // ex : services.TryAddScoped<IWeatherHubService, WeatherHubService>();

            return services;
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.