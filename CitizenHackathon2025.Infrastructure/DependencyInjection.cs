using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Infrastructure.Services;
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









































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.