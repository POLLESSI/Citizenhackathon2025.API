using MapsterMapper;
using CitizenHackathon2025.Application.Mapping;

// ==================
// Namespaces projet (⚠️ harmonized on CitizenHackathon2025)
// ==================
using CitizenHackathon2025.API.Middlewares;
using CitizenHackathon2025.API.Security;
using CitizenHackathon2025.Application.Interfaces;
using OpenAIGptExternalService = CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI.GptExternalService;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs;
using CitizenHackathon2025.Infrastructure.Persistence;
using CitizenHackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.Infrastructure.Services;
using CitizenHackathon2025.API.Extensions;
using CitizenHackathon2025.API.Options;
using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.DTOs;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure;
using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;
using CitizenHackathon2025.Infrastructure.Services.Monitoring;
using CitizenHackathon2025.Infrastructure.SignalR;
using CitizenHackathon2025.Infrastructure.UseCases;
using CitizenHackathon2025.Shared.Interfaces;
using CitizenHackathon2025.Shared.Services;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Dapper;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Serilog;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI;

// =====================================
// Program
// =====================================
internal class Program
{
#nullable disable
    private static void Main(string[] args)
    {
        // ---------- Serilog ----------
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/api-log-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);
        TypeAdapterConfig.GlobalSettings.Scan(AppDomain.CurrentDomain.GetAssemblies());
        builder.Services.AddMapster();
        builder.Host.UseSerilog();

    
        // ---------- Basic setup ----------
        var configuration = builder.Configuration;
        var services = builder.Services;
        var securityEnabled = configuration.GetValue("Security:Enabled", true);
        var require = configuration.GetValue("OutZen:RequireEventId", true);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // ---------- SQL / Dapper ----------
        services.AddSingleton<DbConnectionFactory>(); //1
        services.AddScoped<IDbConnection>(_ => new SqlConnection(configuration.GetConnectionString("default")));
        services.AddScoped<DatabaseService>();
        SqlMapper.AddTypeHandler(new RoleTypeHandler());

        // ---------- Options (OpenAI, OpenWeather, JWT) ----------
        services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
        services.Configure<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>(configuration.GetSection("OpenWeather"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt")); // ← aligned with OutZenTokenMiddleware & JWT

        // Retrieving JWT options for AddJwtBearer
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

        if (builder.Environment.IsDevelopment() && !securityEnabled)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Dev";
                options.DefaultChallengeScheme = "Dev";
            })
           .AddScheme<AuthenticationSchemeOptions, CitizenHackathon2025.API.Security.DevAuthHandler>(
               "Dev", _ => { });
            services.AddAuthorization();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(jwt.Secret))
                throw new InvalidOperationException("JWT Secret is missing or empty. Configure 'Jwt:Secret'.");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var hasIssuer = !string.IsNullOrWhiteSpace(jwt.Issuer);
                var hasAudience = !string.IsNullOrWhiteSpace(jwt.Audience);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret ?? "")),
                    ValidateIssuer = hasIssuer,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = hasAudience,
                    ValidAudience = jwt.Audience,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                // Accept token as querystring for SignalR (WebSockets/SSE)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // 1) Token via querystring for SignalR (WebSockets/SSE)
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // Keep your "guard" routes if you want, but "/hubs" is enough
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs")
                             || path.StartsWithSegments("/hub/outzen") // sympathy if you keep it
                             || path.StartsWithSegments("/aisuggestionhub")))
                        {
                            context.Token = accessToken;
                        }

                        // 2) FALLBACK : token from the HttpOnly cookie (name it like yours)
                        if (string.IsNullOrEmpty(context.Token) &&
                            context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
            services.AddAuthorization();
        }

        // ---------- Auth / JWT ----------
        

        // // Simple variant (commented) :
        // var secretKey = builder.Configuration["JwtSettings:SecretKey"];
        // builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddJwtBearer(options =>
        //     {
        //         options.TokenValidationParameters = new TokenValidationParameters
        //         {
        //             ValidateIssuer = false,
        //             ValidateAudience = false,
        //             ValidateLifetime = true,
        //             ValidateIssuerSigningKey = true,
        //             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        //         };
        //     });

        // // Auth cookies (commented)
        // builder.Services.AddAuthentication("Cookies")
        //     .AddCookie("Cookies", options =>
        //     {
        //         options.Cookie.Name = "AccessToken";
        //         options.LoginPath = "/api/auth/login";
        //         options.AccessDeniedPath = "/api/auth/denied";
        //     });

        // ---------- Domain services / app ----------
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<ICrowdInfoService, CrowdInfoService>();
        services.AddScoped<CrowdInfoService>();
        services.AddScoped<CitizenSuggestionService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IGeoService, GeoService>();
        services.AddScoped<IGPTService, GPTService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<IPasswordHasher, Sha512PasswordHasher>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddSingleton<TokenGenerator>();
        services.AddScoped<ITrafficConditionService, TrafficConditionService>();
        services.AddScoped<ITrafficApiService, TrafficAPIService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<OpenAiSuggestionService>();
        services.AddScoped<TrafficConditionService>();
        services.AddScoped<WeatherSuggestionOrchestrator>();
        //services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserHubService, UserHubService>();
        services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        // ---------- Hubs & notifiers ----------
        services.AddScoped<IWeatherHubService, CitizenHackathon2025.Hubs.Services.WeatherHubService>();
        services.AddScoped<IUserHubService, UserHubService>(); 
        services.AddScoped<IHubNotifier, CitizenHackathon2025.Hubs.Hubs.SignalRNotifier>();

        // ---------- Repositories ----------
        services.AddScoped<IAggregateSuggestionService, AstroIAService>();
        services.AddScoped<IAIRepository, AIRepository>();
        services.AddScoped<ICrowdInfoRepository, CrowdInfoRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IGPTRepository, GPTRepository>();
        services.AddScoped<IGPTRepository, GptInteractionsRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<ISuggestionRepository, SuggestionRepository>();
        services.AddScoped<ITrafficConditionRepository, TrafficConditionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();

        // ---------- Polly : Retry + Circuit Breaker ----------
        services.AddSingleton(sp =>
        {
            var log = sp.GetRequiredService<ILogger<WeatherService>>();

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (ex, delay, count, ctx) =>
                    {
                        log.LogWarning(ex, $"Retry {count} after {delay.TotalSeconds}s");
                    });

            var circuitBreaker = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    3,
                    TimeSpan.FromMinutes(1),
                    onBreak: (ex, delay) => log.LogWarning($"Circuit opened: {ex.Message}"),
                    onReset: () => log.LogInformation("Circuit reset."));

            return Policy.WrapAsync(retryPolicy, circuitBreaker);
        });

        // ---------- Utils & HttpClients ----------
        services.AddSingleton<CspViolationStore>();
        services.AddSingleton<IRealTimeNotifier, RealTimeNotifier>();
        services.AddScoped<IHubNotifier, CitizenHackathon2025.Hubs.Hubs.SignalRNotifier>(); //1 already higher (left for compat)
        services.AddHttpClient();
        services.AddHttpClient("Default")
            .AddPolicyHandler(PollyPolicies.GetResiliencePolicy());

        // HttpClient OpenAI (key from config) → IGptExternalService ↦ OpenAIGptExternalService
        services.AddHttpClient<IGptExternalService, OpenAIGptExternalService>(client =>
        {
            var openAiKey = configuration["OpenAI:ApiKey"];
            client.BaseAddress = new Uri(configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com");
            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", openAiKey);
            }
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        });
        services.AddScoped<AstroIAService>();

        services.AddHttpClient<IOpenWeatherService, OpenWeatherService>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl ?? "https://api.openweathermap.org");
        });
        // ⛔️ Do not duplicate the above record with an AddScoped
        services.AddHttpClient<OpenWeatherMapClient>();
        services.AddMemoryCache();
        services.AddScoped<MemoryCacheService>();
        services.AddScoped<WeatherSuggestionOrchestrator>();
        services.AddHttpClient<ITrafficApiService, TrafficAPIService>(client =>
        {
            client.BaseAddress = new Uri("https://api.waze.com/..."); // TODO: actual URL base
            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        });

        //services.AddHttpClient<OpenWeatherService>()
        //    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)));
        // ⛔️ Unnecessary: ​​another HttpClient<OpenWeatherService> would be redundant and ambiguous
        services.AddInfrastructure();
        services.AddInfrastructureServices();
        services.AddMapster();

        // ---------- MediatR ----------
        services.AddMediatR(typeof(GetLatestForecastQuery).Assembly);
        services.AddMediatR(typeof(GetSuggestionsByUserQuery).Assembly);
        services.AddMediatR(typeof(CitizenHackathon2025.Application.CQRS.Queries.Handlers.GetLatestTrafficConditionQueryHandler).Assembly);

        // NOTE: avoid building a ServiceProvider here (double container)
        // ILogger<Program> logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
        // logger.LogInformation("Application DI built successfully.");

        // ---------- CORS ----------
        services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazor", p =>
                p.WithOrigins("https://localhost:7101")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials());
        });

        // ---------- Controllers / JSON ----------
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
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

        // ---------- SignalR ----------
        services.AddSignalR(options => { options.EnableDetailedErrors = true; });

        // ---------- Hosted Services ----------
        services.AddHostedService<EventArchiverService>();
        services.AddHostedService<WeatherService>();

        // ---------- Autorisation ----------
        services.AddAuthorization(o =>
        {
            o.AddPolicy("Admin", p => p.RequireRole(Roles.Admin));
            o.AddPolicy("Modo", p => p.RequireRole(Roles.Admin, Roles.Modo));
            o.AddPolicy("User", p => p.RequireRole(Roles.User));
            o.AddPolicy("Guest", p => p.RequireRole(Roles.Guest));
        });

        // ---------- Swagger ----------
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
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
                    Array.Empty<string>()
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



        //services.AddHttpClient<ChatGptService>();

        // (Old hard version → we keep commented)
        //services.AddHttpClient<GptExternalService>(client =>
        //{
        //    client.DefaultRequestHeaders.Authorization =
        //        new AuthenticationHeaderValue("Bearer", "sk-xxxxxxxxxxxxxxxx");
        //});

        // === User DI registrations (fully qualified to avoid any using parasite) ===
        services.AddScoped<
            CitizenHackathon2025.Application.Interfaces.IUserService,
            CitizenHackathon2025.Infrastructure.Services.UserService>();

        services.AddScoped<
            CitizenHackathon2025.Domain.Interfaces.IUserRepository,
            CitizenHackathon2025.Infrastructure.Repositories.UserRepository>();

        services.AddScoped<
            CitizenHackathon2025.Application.Interfaces.IUserHubService,
            CitizenHackathon2025.Infrastructure.Services.UserHubService>();



        // ---------- Build app ----------
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            // These GetRequiredService must pass without throw :
            _ = scope.ServiceProvider.GetRequiredService<
                CitizenHackathon2025.Application.Interfaces.IUserService>();

            _ = scope.ServiceProvider.GetRequiredService<
                CitizenHackathon2025.Domain.Interfaces.IUserRepository>();

            _ = scope.ServiceProvider.GetRequiredService<
                CitizenHackathon2025.Application.Interfaces.IUserHubService>();
        }

        SqlMapper.AddTypeHandler(new RoleTypeHandler());

        

        // Test DI
        using (var scope = app.Services.CreateScope())
        {
            var test = scope.ServiceProvider.GetRequiredService<CitizenSuggestionService>();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/", ctx =>
            {
                ctx.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });
        }

        if (!app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                var ua = context.Request.Headers["User-Agent"].ToString();

                // Allows browsers + dev tools
                var allowed = new[] { "Mozilla", "Chrome", "Edge", "Safari", "curl", "Postman", "Insomnia", "Edge" };
                var ok = !string.IsNullOrWhiteSpace(ua)
                         && allowed.Any(a => ua.Contains(a, StringComparison.OrdinalIgnoreCase));

                if (!ok)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Forbidden - Invalid User-Agent");
                    return;
                }

                await next();
            });
        }

        //if (app.Environment.IsProduction())
        //{
        //    app.Use(async (context, next) =>
        //    {
        //        context.Response.Headers["X-API-Copyright"] = "© 2025 POLLESSI / CitizenHackathon2025. Reproduction prohibited.";
        //        await next.Invoke();
        //    });
        //}

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        var enableSwagger = app.Configuration.GetValue<bool?>("Swagger:Enabled")
                   ?? app.Environment.IsDevelopment();

        if (enableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CitizenHackathon2025 API V1");
                c.RoutePrefix = "swagger"; // so the UI will be on /swagger
            });

            // Optional: Redirect root to Swagger in Dev
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/", ctx =>
                {
                    ctx.Response.Redirect("/swagger");
                    return Task.CompletedTask;
                });
            }
        }

        // Middlewares custom
        app.UseExceptionMiddleware();
        app.UseAntiXssMiddleware();
        app.UseSecurityHeaders();
        app.UseUserAgentFiltering();
        app.UseAuditLogging();

        app.UseRouting();

        app.UseCors("AllowBlazor");
        app.UseAuthentication();

        // ⇩⇩⇩ ONLY applies this to /api (not /swagger, /, /static, etc.)
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
            b => b.UseMiddleware<OutZenTokenMiddleware>());

        app.UseAuthorization();

        app.MapControllers();

        // ---------- Hubs ----------
        var hubs = app.MapGroup("/hubs");
        hubs.MapHub<AISuggestionHub>("/aisuggestionhub");
        hubs.MapHub<CrowdHub>("/crowdHub").RequireAuthorization();
        hubs.MapHub<EventHub>("/eventHub");
        hubs.MapHub<NotificationHub>("/notifications");
        hubs.MapHub<OutZenHub>("/outzen");
        hubs.MapHub<PlaceHub>("/placeHub");
        hubs.MapHub<SuggestionHub>("/suggestionHub");
        hubs.MapHub<TrafficHub>("/trafficHub");
        hubs.MapHub<UpdateHub>("/updateHub");
        hubs.MapHub<UserHub>("/userHub");
        hubs.MapHub<WeatherForecastHub>("/weatherforecastHub");


        // (Alternative without group)
        //app.MapHub<AISuggestionHub>("/aisuggestionhub");
        //app.MapHub<CrowdHub>("/hubs/crowdHub");
        //app.MapHub<EventHub>("/hubs/eventHub");
        //app.MapHub<NotificationHub>("/hubs/notifications");
        //app.MapHub<OutZenHub>("/hub/outzen"); // <- consistent with JwtBearerEvents
        //app.MapHub<PlaceHub>("/hubs/placeHub");
        //app.MapHub<SuggestionHub>("/hubs/suggestionHub");
        //app.MapHub<TrafficHub>("/hubs/trafficHub");
        //app.MapHub<UpdateHub>("/hubs/updateHub");
        //app.MapHub<UserHub>("/hubs/userHub");
        //app.MapHub<WeatherForecastHub>("/hubs/weatherforecastHub");
        //app.MapHub<CitizeHackathon2025.Hubs.Hubs.WeatherForecastHub>("/hubs/weatherforecastHub"); // (original typo)

        app.MapGet("/auth/hub-token", (HttpContext http, TokenGenerator tokens) =>
        {
            // Variant A (simple): If your cookie carries a valid JWT, OnMessageReceived will have already accepted it.
            // Since the endpoint is RequireAuthorization(), the user is authenticated here:
            if (http.User?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            // From the authenticated identity, you issue a short-lived "hub" token (e.g. 5 mins)
            // Adapt the TokenGenerator API to yours:
            var hubToken = tokens.GenerateTokenFromPrincipal(http.User, expiresInMinutes: 5);

            return Results.Ok(new { token = hubToken });
        })
        .RequireAuthorization();

        app.MapGet("/trafficcondition/latest",
            async (IMediator mediator, CancellationToken ct) =>
            {
                var list = await mediator.Send(new GetLatestTrafficConditionQuery(), ct);
                return (list is null || list.Count == 0) ? Results.NotFound() : Results.Ok(list);
            });

        // Small query log (as before)
        app.Use(async (context, next) =>
        {
            Console.WriteLine($"Request {context.Request.Method} {context.Request.Path}");
            await next.Invoke();
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