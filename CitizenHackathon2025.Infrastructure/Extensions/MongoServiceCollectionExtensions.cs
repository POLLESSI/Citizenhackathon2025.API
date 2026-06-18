using CitizenHackathon2025.Infrastructure.NoSql.Mongo;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories;
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

            services.AddScoped<IGptInteractionNoSqlRepository, GptInteractionNoSqlRepository>();

            return services;
        }
    }
}
