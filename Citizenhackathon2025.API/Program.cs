using AutoMapper;
using Azure.Core.Pipeline;
using Citizenhackathon2025.API.Security;
using Citizenhackathon2025.Application.CQRS.Commands.Handlers;
using Citizenhackathon2025.Application.CQRS.Queries.Handlers;
using Citizenhackathon2025.Application.Extensions;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application.Services;
using Citizenhackathon2025.Application.WeatherForecast.Commands;
using Citizenhackathon2025.Application.WeatherForecast.Handlers;
using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Application.UseCases;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Infrastructure.ExternalAPIs;
using Citizenhackathon2025.Infrastructure.Persistence;
using Citizenhackathon2025.Infrastructure.Repositories;
using Citizenhackathon2025.Infrastructure.Repositories.Providers.Hubs;
using Citizenhackathon2025.Infrastructure.Services;
using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Hubs.Services;
using CityzenHackathon2025.API.Tools;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.VisualStudio.Services.CircuitBreaker;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Wrap;
using System.Data;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

#nullable disable
// Add services to the container.

// SQLConnection

builder.Services.AddScoped<System.Data.IDbConnection>(static sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("default");
    return new SqlConnection(connectionString);
});

// Authentications

var secretKey = builder.Configuration["JwtSettings:SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    }); 

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "AccessToken";
        options.LoginPath = "/api/auth/login";
        options.AccessDeniedPath = "/api/auth/denied";
    });

// Injections

builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<ICrowdInfoRepository, CrowdInfoRepository>();
builder.Services.AddScoped<ICrowdInfoService, CrowdInfoService>();
//builder.Services.AddScoped<CitizenHackathon2025>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IGPTRepository, GPTRepository>();
builder.Services.AddScoped<IGptInteractionRepository, GptInteractionsRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
builder.Services.AddScoped<ISuggestionRepository, SuggestionRepository>();
builder.Services.AddScoped<ISuggestionService, SuggestionService>();
builder.Services.AddSingleton<TokenGenerator>();
builder.Services.AddScoped<ITrafficConditionService, TrafficConditionService>();
builder.Services.AddScoped<ITrafficConditionRepository, TrafficConditionRepository>();
//builder.Services.AddScoped<ITrafficApiService, TrafficAPIService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserHubService, UserHubService>();
builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();
builder.Services.AddScoped<IWeatherHubService, CitizenHackathon2025.Hubs.Services.WeatherHubService>();
builder.Services.AddSingleton<AsyncPolicyWrap>(sp =>
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
builder.Services.AddSingleton<TokenGenerator>();

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<IHubNotifier, Citizenhackathon2025.Hubs.Hubs.SignalRNotifier>();
builder.Services.AddMediatR(typeof(GetLatestForecastQuery).Assembly);
//builder.Services.AddSingleton<AsyncPolicyWrap<HttpResponseMessage>>(PollyPolicies.GetResiliencePolicy()); 

builder.Services.AddHttpClient();
//builder.Services
//    .AddHttpClient<IAIService, ChatGptService>();
//    .AddPolicyHandler(sp => sp.GetRequiredService<AsyncPolicyWrap<HttpResponseMessage>>());

//builder.Services
//    .AddHttpClient<Citizenhackathon2025.Application.Interfaces.IOpenWeatherMapService, OpenWeatherMapService>();
//    .AddPolicyHandler(sp => sp.GetRequiredService<AsyncPolicyWrap<HttpResponseMessage>>());
builder.Services.AddHttpClient("Default")
    .AddPolicyHandler(PollyPolicies.GetResiliencePolicy());
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient<IAIService, AIService>();
builder.Services.AddHttpClient<IOpenWeatherService, Citizenhackathon2025.Infrastructure.Services.OpenWeatherService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CitizenHackathon2025.Application.Services.OpenWeatherService>();
builder.Services.AddScoped<MemoryCacheService>();
builder.Services.AddScoped<OpenAiSuggestionService>();
builder.Services.AddScoped<WeatherSuggestionOrchestrator>();
builder.Services.AddHttpClient<OpenWeatherMapClient>();
//builder.Services.AddHttpClient<ITrafficApiService, TrafficAPIService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Replace the ambiguous line with the following explicit call to resolve the ambiguity:
//AutoMapper.ServiceCollectionExtensions.AddAutoMapper(builder.Services, config => { }, AppDomain.CurrentDomain.GetAssemblies());
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
//builder.Services.AddScoped<IAIService, AIService>();
//builder.Services.AddScoped<IAIService>(sp =>
//{
//    var config = sp.GetRequiredService<IConfiguration>();
//    var apiKey = config.GetValue<string>("OpenAI:ApiKey");
//    return new AIService(apiKey);
//});
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
builder.Services.AddSignalR();
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
    options.AddPolicy("AllowAnyOrigin", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              /*.WithOrigins("https://monsite.com")*/;
    });
});

// Token Generator

builder.Services.AddScoped<TokenGenerator>();

// Security levels
// Declaration of the different security levels to be implemented in the controller using the attribute [Authorize("font_name")]
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
    o.AddPolicy("Modo", policy => policy.RequireClaim("role", "admin", "modo"));
    o.AddPolicy("User", policy => policy.RequireClaim("role", "user"));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
});

builder.Services.AddHttpClient<ChatGptService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
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

// Common

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseMiddleware<Citizenhackathon2025.API.Security.AntiXssMiddleware>();
app.UseStaticFiles();
//app.UseMiddleware<Citizenhackathon2025.API.Middlewares.ExceptionMiddleware>();

app.UseRouting();
app.UseAntiXssMiddleware(); // Protection XSS
//app.UseCustomExceptionMiddleware();

app.UseCors("AllowAnyOrigin");

app.UseAuthorization();
app.MapControllers();

app.UseEndpoints(Endpoints =>
{
    Endpoints.MapControllers();

    Endpoints.MapHub<EventHub>("/hubs/eventHub");
    Endpoints.MapHub<NotificationHub>("/hubs/notifications");
    Endpoints.MapHub<PlaceHub>("/hubs/placeHub");
    Endpoints.MapHub<SuggestionHub>("/hubs/suggestionHub");
    Endpoints.MapHub<TrafficHub>("/hubs/trafficHub");
    Endpoints.MapHub<UpdateHub>("/hubs/updateHub");
    Endpoints.MapHub<UserHub>("/hubs/userHub");
    Endpoints.MapHub<CrowdHub>("/hubs/crowdHub");
    Endpoints.MapHub<CitizeHackathon2025.Hubs.Hubs.WeatherForecastHub>("/hubs/weatherforecastHub");
});

app.MapGet("/api/weatherforecast", () =>
{
    var rng = new Random();
    return new Citizenhackathon2025.Domain.Entities.WeatherForecast
    {
        DateWeather = DateTime.Now,
        TemperatureC = rng.Next(-20, 55),
        Summary = "Static",
        RainfallMm = rng.Next(0, 100),
        Humidity = rng.Next(30, 100),
        WindSpeedKmh = rng.Next(0, 200) * 100
    };
});

static void UseSecurityHeaders(IApplicationBuilder app)
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        await next();
    });
}
UseSecurityHeaders(app);

//app.MapControllers();

app.Run();
