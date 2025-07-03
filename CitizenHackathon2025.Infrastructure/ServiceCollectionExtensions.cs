using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CitizenHackathon2025.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"] ?? "CitizenHackathon2025API";

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
                    };
                });

            services.AddAuthorization(o =>
            {
                o.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
                o.AddPolicy("Modo", policy => policy.RequireClaim("role", "admin", "modo"));
                o.AddPolicy("User", policy => policy.RequireClaim("role", "user"));
            });

            return services;
        }

        // other methods for services Database, Application, Infrastructure, Hubs, HttpClients, Polly, Swagger, etc.
    }
}
