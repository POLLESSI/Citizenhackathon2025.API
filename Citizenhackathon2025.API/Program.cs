using Azure.Core.Pipeline;
//using Mapster.DependencyInjection;
//using Citizenhackathon2025.Application.Mapping;
using Citizenhackathon2025.API.Middlewares;
using Citizenhackathon2025.API.Security;
using Citizenhackathon2025.Application.CQRS.Commands.Handlers;
using Citizenhackathon2025.Application.CQRS.Queries.Handlers;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application.WeatherForecast.Commands;
using Citizenhackathon2025.Application.WeatherForecast.Handlers;
using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Infrastructure.ExternalAPIs;
using Citizenhackathon2025.Infrastructure.Persistence;
using Citizenhackathon2025.Infrastructure.Repositories;
using Citizenhackathon2025.Infrastructure.Repositories.Providers.Hubs;
using Citizenhackathon2025.Infrastructure.Services;
using CitizenHackathon2025.API.Extensions;
using CitizenHackathon2025.API.Middlewares;
using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure;
using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;
using CitizenHackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.Infrastructure.Services;
using CitizenHackathon2025.Infrastructure.Services.Monitoring;
using CitizenHackathon2025.Infrastructure.SignalR;
using CitizenHackathon2025.Infrastructure.UseCases;
using CitizenHackathon2025.Shared.Interfaces;
using CitizenHackathon2025.Shared.Services;
using CityzenHackathon2025.API.Tools;
using Dapper;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.VisualStudio.Services.CircuitBreaker;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Wrap;
using Serilog;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http.Headers;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/api-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();


        var builder = WebApplication.CreateBuilder(args);

    #nullable disable
        // 1. Configuration
        var configuration = builder.Configuration;
        var services = builder.Services;
        // 2. Add Application + Infrastructure layers



        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        // SQLConnection
        builder.Services.AddSingleton<DbConnectionFactory>();
        builder.Services.AddScoped<IDbConnection>(static sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("default");
            return new System.Data.SqlClient.SqlConnection(connectionString);
        });
        builder.Services.AddScoped<DatabaseService>();

        // Authentications

        // Secret key (to be put in appsettings.json or secrets in production)
        var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
        var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "CitizenHackathon2025API";

        builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hub/outzen"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

        //var secretKey = builder.Configuration["JwtSettings:SecretKey"];

        //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //    .AddJwtBearer(options =>
        //    {
        //        options.TokenValidationParameters = new TokenValidationParameters
        //        {
        //            ValidateIssuer = false,
        //            ValidateAudience = false,
        //            ValidateLifetime = true,
        //            ValidateIssuerSigningKey = true,
        //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        //        };
        //    }); 

        //builder.Services.AddAuthentication("Cookies")
        //    .AddCookie("Cookies", options =>
        //    {
        //        options.Cookie.Name = "AccessToken";
        //        options.LoginPath = "/api/auth/login";
        //        options.AccessDeniedPath = "/api/auth/denied";
        //    });

        // Injections
        // ========== DOMAIN SERVICES ==========
        builder.Services.AddScoped<IAIService, AIService>();
        builder.Services.AddScoped<ICrowdInfoService, CrowdInfoService>();
        builder.Services.AddScoped<CrowdInfoService>();
        builder.Services.AddScoped<CitizenSuggestionService>();
        builder.Services.AddScoped<DatabaseService>();
        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddScoped<IGeoService, GeoService>();
        builder.Services.AddScoped<IGPTService, GPTService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IPlaceService, PlaceService>();
        builder.Services.AddScoped<IPasswordHasher, Sha512PasswordHasher>();
        builder.Services.AddScoped<ISuggestionService, SuggestionService>();
        builder.Services.AddSingleton<TokenGenerator>();
        builder.Services.AddScoped<ITrafficConditionService, TrafficConditionService>();
        builder.Services.AddScoped<ITrafficApiService, TrafficAPIService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<OpenAiSuggestionService>();
        builder.Services.AddScoped<OpenWeatherService>();
        builder.Services.AddScoped<TrafficConditionService>();
        builder.Services.AddScoped<WeatherSuggestionOrchestrator>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IUserHubService, UserHubService>();
        builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        // ========== HUBS & NOTIFIERS ==========
        builder.Services.AddScoped<IWeatherHubService, CitizenHackathon2025.Hubs.Services.WeatherHubService>();
        builder.Services.AddScoped<IUserHubService, UserHubService>();
        builder.Services.AddScoped<IHubNotifier, Citizenhackathon2025.Hubs.Hubs.SignalRNotifier>();
        // ========== REPOSITORIES ==========
        services.AddScoped<IAggregateSuggestionService, AstroIAService>();
        builder.Services.AddScoped<IAIRepository, AIRepository>();
        builder.Services.AddScoped<ICrowdInfoRepository, CrowdInfoRepository>();
        builder.Services.AddScoped<IEventRepository, EventRepository>();
        builder.Services.AddScoped<IGPTRepository, GPTRepository>();
        builder.Services.AddScoped<IGPTRepository, GptInteractionsRepository>();
        builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
        builder.Services.AddScoped<ISuggestionRepository, SuggestionRepository>();
        builder.Services.AddScoped<ITrafficConditionRepository, TrafficConditionRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();

        // ========== POLLY: RETRY + CIRCUIT BREAKER ==========
        builder.Services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<WeatherService>>();

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (ex, delay, count, ctx) =>
                    {
                        logger.LogWarning(ex, $"Retry {count} after {delay.TotalSeconds}s");
                    });

            var circuitBreaker = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    3,
                    TimeSpan.FromMinutes(1),
                    onBreak: (ex, delay) => logger.LogWarning($"Circuit opened: {ex.Message}"),
                    onReset: () => logger.LogInformation("Circuit reset."));

            return Policy.WrapAsync(retryPolicy, circuitBreaker);
        });
        // ========== UTILS ==========

        // Token generator
        builder.Services.AddSingleton<TokenGenerator>();
        // DB Connection Factory
        builder.Services.AddSingleton<DbConnectionFactory>();

        builder.Services.AddSingleton<CspViolationStore>();

        services.AddSingleton<IRealTimeNotifier, RealTimeNotifier>();

        builder.Services.AddScoped<IHubNotifier, Citizenhackathon2025.Hubs.Hubs.SignalRNotifier>();

        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("Default")
            .AddPolicyHandler(PollyPolicies.GetResiliencePolicy());
        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IOptions<OpenAIOptions>>().Value);
        builder.Services.AddHttpClient<IAIService, AIService>();
        builder.Services.AddHttpClient<IOpenWeatherService, OpenWeatherService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<OpenWeatherService>>();
            var apiKey = config["OpenWeather:ApiKey"] ?? throw new InvalidOperationException("API key is not configured.");
            var baseUrl = config["OpenWeather:BaseUrl"] ?? "https://api.openweathermap.org";

        });

        builder.Services.AddScoped<IOpenWeatherService, OpenWeatherService>();
        builder.Services.Configure<OpenWeatherOptions>(builder.Configuration.GetSection("OpenWeather"));
        builder.Services.AddHttpClient<IOpenWeatherService, OpenWeatherService>();

        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<MemoryCacheService>();
        builder.Services.AddScoped<OpenAiSuggestionService>();
        builder.Services.AddScoped<WeatherSuggestionOrchestrator>();
        builder.Services.AddHttpClient<OpenWeatherMapClient>();
        builder.Services.AddHttpClient<IOpenWeatherService, OpenWeatherService>();
        builder.Services.AddHttpClient<GptExternalService>(client =>
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "TA_CLE_OPENAI");
        });

        builder.Services.AddScoped<AstroIAService>();
        builder.Services.AddScoped<GptExternalService>();
        builder.Services.AddHttpClient<ITrafficApiService, TrafficAPIService>(client =>
        {
            client.BaseAddress = new Uri("https://api.waze.com/...");
            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        });
        services.AddHttpClient<OpenWeatherService>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)));
        builder.Services.AddInfrastructure();
        builder.Services.AddInfrastructureServices();
        builder.Services.AddMapster();
        // ========== MEDIATR ==========
        builder.Services.AddMediatR(typeof(GetLatestForecastQuery).Assembly);
        builder.Services.AddMediatR(typeof(GetSuggestionsByUserQuery).Assembly);

        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

        ILogger<Program> logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Application DI built successfully.");

        // ========== LOGGING ==========
        builder.Logging.AddConsole();


        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = null;

            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // to return a uniform response on validation errors
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .Select(e => new
                        {
                            Field = e.Key,
                            Errors = e.Value.Errors.Select(err => err.ErrorMessage)
                        });

                    return new BadRequestObjectResult(new { Message = "Validation failed", Errors = errors });
                };
            });

        // SignalR
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });
        builder.Services.AddHostedService<EventArchiverService>();
        builder.Services.AddHostedService<WeatherService>();

        // Add Hubs

        //builder.Services.AddSingleton<CrowdHub>();
        //builder.Services.AddSingleton<EventHub>();
        //builder.Services.AddSingleton<GPTHub>();
        //builder.Services.AddSingleton<PlaceHub>();
        //builder.Services.AddSingleton<SuggestionHub>();
        //builder.Services.AddSingleton<TrafficHub>();
        //builder.Services.AddSingleton<UpdateHub>();
        //builder.Services.AddSingleton<UserHub>();
        //builder.Services.AddScoped<CitizeHackathon2025.Hubs.Hubs.WeatherForecastHub>();

        // Connection

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://localhost:7254", "https://localhost:7051")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Security levels
        // Declaration of the different security levels to be implemented in the controller using the attribute [Authorize("font_name")]
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

        builder.Services.AddAuthorization(o =>
        {
            o.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
            o.AddPolicy("Modo", policy => policy.RequireClaim("role", "admin", "modo"));
            o.AddPolicy("User", policy => policy.RequireClaim("role", "user"));
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
            new string[] {}
        }
            });
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "CitizenHackathon2025", Version = "v1" });
            //c.SwaggerDoc("v1", new OpenApiInfo
            //{
            //    Title = "CitizenHackathon2025 API",
            //    Version = "v1",
            //    Description = "© 2025 POLLESSI / CitizenHackathon2025 — Protected private API. Any attempt at usurpation or unauthorized use will be prosecuted."
            //});
        });

        builder.Services.AddHttpClient<ChatGptService>();

        services.AddHttpClient<GptExternalService>(client =>
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "sk-xxxxxxxxxxxxxxxx");
        });

        var app = builder.Build();

        SqlMapper.AddTypeHandler(new RoleTypeHandler());

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CitizenHackathon2025 API V1");
                c.RoutePrefix = "swagger";
            });
        }
        else
        {
            // Swagger disabled in production for security reasons
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        // Test DI
        using (var scope = app.Services.CreateScope())
        {
            var test = scope.ServiceProvider.GetRequiredService<CitizenSuggestionService>();
        }
        //if (app.Environment.IsProduction())
        //{
        //    app.Use(async (context, next) =>
        //    {
        //        context.Response.Headers["X-API-Copyright"] = "© 2025 POLLESSI / CitizenHackathon2025. Reproduction prohibited.";
        //        await next.Invoke();
        //    });
        //}

        // Common

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        //app.UseMiddleware<AntiXssMiddleware>();
        app.UseExceptionMiddleware();
        app.UseAntiXssMiddleware();
        app.UseSecurityHeaders();
        app.UseUserAgentFiltering();
        app.UseAuditLogging();
        app.UseMiddleware<OutZenTokenMiddleware>();
        //app.UseMiddleware<ExceptionMiddleware>();

        app.UseCors();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        app.MapHub<EventHub>("/hubs/eventHub");
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<OutZenHub>("/hub/outzen");
        app.MapHub<PlaceHub>("/hubs/placeHub");
        app.MapHub<SuggestionHub>("/hubs/suggestionHub");
        app.MapHub<TrafficHub>("/hubs/trafficHub");
        app.MapHub<UpdateHub>("/hubs/updateHub");
        app.MapHub<UserHub>("/hubs/userHub");
        app.MapHub<CrowdHub>("/hubs/crowdHub");
        app.MapHub<AISuggestionHub>("/aisuggestionhub");
        app.MapHub<CitizeHackathon2025.Hubs.Hubs.WeatherForecastHub>("/hubs/weatherforecastHub");

        app.MapGet("/api/weatherforecast", () =>
        {
            var rng = new Random();
            return new WeatherForecast
            {
                DateWeather = DateTime.Now,
                TemperatureC = rng.Next(-20, 55),
                Summary = "Static",
                RainfallMm = rng.Next(0, 100),
                Humidity = rng.Next(30, 100),
                WindSpeedKmh = rng.Next(0, 200) * 100
            };
        });
        //app.Use(async (context, next) =>
        //{
        //    context.Response.Headers.Add("X-API-Copyright", "© 2025 POLLESSI / CitizenHackathon2025. All rights reserved.");
        //    await next.Invoke();
        //});

        //static void UseSecurityHeaders(IApplicationBuilder app)
        //{
        //    app.Use(async (context, next) =>
        //    {
        //        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
        //        {
        //            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        //        }
        //        context.Response.Headers.Add("X-Frame-Options", "DENY");
        //        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        //        await next();
        //    });
        //}
        //UseSecurityHeaders(app);

        //app.MapControllers();

        app.Run();
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.