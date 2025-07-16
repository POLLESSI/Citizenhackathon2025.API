using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.Infrastructure.Services;

namespace CitizenHackathon2025.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IAIRepository, AIRepository>();
            services.AddScoped<IAIService, AIService>();
            services.AddScoped<IGPTService, GPTService>();

            return services;
        }
    }
}





































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.