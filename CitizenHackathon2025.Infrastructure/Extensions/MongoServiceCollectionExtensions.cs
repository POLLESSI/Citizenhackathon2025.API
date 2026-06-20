using CitizenHackathon2025.Infrastructure.NoSql.Mongo;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Options;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenHackathon2025.Infrastructure.Extensions
{
    public static class MongoServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoPersistence(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoOptions>(configuration.GetSection("MongoDb"));

            services.AddSingleton<IMongoDbContext, MongoDbContext>();
            services.AddScoped<ICrowdSnapshotRepository, CrowdSnapshotRepository>();
            services.AddScoped<IGptInteractionNoSqlRepository, GptInteractionNoSqlRepository>();
            services.AddScoped<ITrafficSnapshotRepository, TrafficSnapshotRepository>();
            services.AddScoped<ISignalRDiagnosticRepository, SignalRDiagnosticRepository>();
            services.AddScoped<INoSqlArchivingService, NoSqlArchivingService>();
            services.AddScoped<IWeatherSnapshotRepository, WeatherSnapshotRepository>();
            services.AddScoped<IMongoSnapshotWriter, MongoSnapshotWriter>();

            return services;
        }
    }
}


















































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.