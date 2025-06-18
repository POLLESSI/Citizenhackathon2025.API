using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Infrastructure.Services;
using CitizenHackathon2025.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenHackathon2025.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IOpenWeatherService, OpenWeatherService>();
            return services;
        }
    }
}
