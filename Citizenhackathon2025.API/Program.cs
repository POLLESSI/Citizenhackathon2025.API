// ==================
// Namespaces projet (⚠️ harmonized on CitizenHackathon2025)
// ==================
using CitizenHackathon2025.API.Extensions;
using CitizenHackathon2025.API.Hubs.Serilog.Sinks;
using CitizenHackathon2025.API.Middlewares;
using CitizenHackathon2025.API.Options;
using CitizenHackathon2025.API.Security;
using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.Behaviors;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.DTOs;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Mapping;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Extensions;
using CitizenHackathon2025.Hubs.Filters;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure;
using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;
using CitizenHackathon2025.Infrastructure.ExternalAPIs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI;
using CitizenHackathon2025.Infrastructure.Persistence;
using CitizenHackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.Infrastructure.Services;
using CitizenHackathon2025.Infrastructure.Services.Monitoring;
using CitizenHackathon2025.Infrastructure.SignalR;
using CitizenHackathon2025.Infrastructure.UseCases;
using CitizenHackathon2025.Shared.Interfaces;
using CitizenHackathon2025.Shared.Notifications;
using CitizenHackathon2025.Shared.Resilience;
using CitizenHackathon2025.Shared.Services;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Dapper;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Instrumentation;
//using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Wrap;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using OpenAIGptExternalService = CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI.GptExternalService;

// =====================================
// Program
// =====================================
internal class Program
{
#nullable disable
    private static void Main(string[] args)
    {
        // ---------- Serilog ----------
        var builder = WebApplication.CreateBuilder(args);

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: "CitizenHackathon2025.API", serviceVersion: "1.0.0");

        Log.Logger = new LoggerConfiguration()
            .Destructure.ByTransforming<LogsDTO>(x => new {
                x.Id,
                Sensitive = "***"
            })
            .CreateLogger();

        TypeAdapterConfig.GlobalSettings.Scan(AppDomain.CurrentDomain.GetAssemblies());
        builder.Services.AddMapster();

        // OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("CitizenHackathon2025.API"))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://localhost:4317")))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
            // ⚠️ NO export Prometheus here
            );

        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("App", "CitizenHackathon2025.API");

            var cs = ctx.Configuration["EventHubs:ConnectionString"];
            if (!string.IsNullOrWhiteSpace(cs))
            {
                var opt = new AzureEventHubOptions
                {
                    ConnectionString = cs,
                    EventHubName = ctx.Configuration["EventHubs:EventHubName"],
                    BatchSizeLimit = ctx.Configuration.GetValue("EventHubs:BatchSizeLimit", 100),
                    Period = TimeSpan.FromSeconds(ctx.Configuration.GetValue("EventHubs:PeriodSeconds", 2)),
                    PartitionKeyResolver = e =>
                        (e.Properties.TryGetValue("CorrelationId", out var cid) ? $"{e.Level}-{cid}" : e.Level.ToString())
                };

                lc.WriteTo.AzureEventHub(opt, new CompactJsonFormatter());
            }
        });
        builder.Logging.ClearProviders();

        // ---------- Basic setup ----------
        var configuration = builder.Configuration;
        var services = builder.Services;
        services.AddOptions<CitizenHackathon2025.Shared.Options.SecurityOptions>()
            .Bind(configuration.GetSection("Security"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.PromptHashPepper),
                "Missing Security:PromptHashPepper in configuration.")
            .ValidateOnStart();
        var securityEnabled = configuration.GetValue("Security:Enabled", true);
        var require = configuration.GetValue("OutZen:RequireEventId", true);

        var pepper = configuration["Security:PromptHashPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException("Missing Security:PromptHashPepper in configuration (startup).");
        }

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
                    OnMessageReceived = ctx =>
                    {
                        var path = ctx.HttpContext.Request.Path;
                        var fromQuery = ctx.Request.Query["access_token"];
                        var fromCookie = ctx.Request.Cookies.TryGetValue("access_token", out var cookie) ? cookie : null;

                        if (path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(fromQuery))
                            ctx.Token = fromQuery;  // OK for SignalR
                        else if (!string.IsNullOrEmpty(fromCookie))
                            ctx.Token = fromCookie; // Classic API (if you accept cookies)

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
        //services.AddScoped<IGPTRepository, GPTRepository>();
        services.AddScoped<IGPTRepository, GptInteractionsRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<ISuggestionRepository, SuggestionRepository>();
        services.AddScoped<ITrafficConditionRepository, TrafficConditionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
        services.AddSingleton<INotifierAdmin, NotifierAdmin>();

        // ---------- Polly : Retry + Circuit Breaker ----------
        services.AddSingleton(sp =>
        {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Polly");
            var notifier = sp.GetRequiredService<INotifierAdmin>(); // <- injects the notifier

            return new
            {
                OpenAi = Resilience.BuildHttpPipeline(log, notifier, "openai", retry: 3, breakerFailures: 5, openSeconds: 30, timeoutSeconds: 25),
                Weather = Resilience.BuildHttpPipeline(log, notifier, "openweather", retry: 2, breakerFailures: 4, openSeconds: 20, timeoutSeconds: 8),
                Traffic = Resilience.BuildHttpPipeline(log, notifier, "trafficapi", retry: 3, breakerFailures: 5, openSeconds: 30, timeoutSeconds: 10),
            };
        });

        // Specific policy for OpenWeatherService (used in WeatherService)
        services.AddSingleton<AsyncPolicyWrap>(sp =>
        {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("WeatherPolicy");

            var retry = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (ex, delay, attempt, ctx) =>
                        log.LogWarning(ex, "[WeatherService] Retry {Attempt} after {Delay}s", attempt, delay.TotalSeconds));

            var breaker = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (ex, breakDelay) =>
                        log.LogWarning("[WeatherService] Circuit open for {Delay}s due to {Message}", breakDelay.TotalSeconds, ex.Message),
                    onReset: () =>
                        log.LogInformation("[WeatherService] Circuit closed. Normal operations resumed."));

            // (optional) non-generic timeout
            // var timeout = Policy.TimeoutAsync(20);

            // Compose what you use in WeatherService
            return Policy.WrapAsync(retry, breaker /*, timeout */);
        });

        // ---------- Utils & HttpClients ----------
        services.AddSingleton<CspViolationStore>();
        services.AddSingleton<IRealTimeNotifier, RealTimeNotifier>();
        services.AddScoped<IHubNotifier, CitizenHackathon2025.Hubs.Hubs.SignalRNotifier>(); //1 already higher (left for compat)
        services.AddHttpClient();
        services.AddHttpClient("Default")
            .AddPolicyHandler(PollyPolicies.GetResiliencePolicy());

        // HttpClient OpenAI (key from config) → IGptExternalService ↦ OpenAIGptExternalService
        services.AddHttpClient<IGptExternalService>(client =>
        {
            var openAiKey = configuration["OpenAI:ApiKey"];
            client.BaseAddress = new Uri(configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com");
            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", openAiKey);
            }
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        })
        .AddHttpMessageHandler(sp =>
        {
            var pipelines = sp.GetRequiredService<dynamic>();
            var logger = sp.GetRequiredService<ILogger<ResilienceHandler>>();
            return new ResilienceHandler(pipelines.OpenAi, logger);
        });

        services.AddSingleton(sp =>
        {
            var lf = sp.GetRequiredService<ILoggerFactory>();
            var log = lf.CreateLogger("PollyPoliciesV7");
            return PollyPoliciesV7.Build("openai", log);
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
        services.AddHttpClient<ITrafficApiService, TrafficAPIService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["TrafficApi:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
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
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResilienceBehavior<,>));

        // ---------- Anti-forgery ----------
        services.AddAntiforgery(o =>
        {
            o.Cookie.Name = "XSRF-TOKEN";
            o.Cookie.HttpOnly = false; // lisible JS pour double-submit
            o.HeaderName = "X-XSRF-TOKEN";
        });

        // ---------- Rate Limiter (Token Bucket) ----------
        services.AddRateLimiter(_ => _
            .AddPolicy("per-user", http =>
            {
                var userId = http.User?.Identity?.Name ?? http.Connection.RemoteIpAddress?.ToString() ?? "anon";
                return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,                      // burst
                    TokensPerPeriod = 100,                 // ✅ remplace RefillTokens
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1), // ✅ remplace RefillPeriod
                    AutoReplenishment = true,              // ✅ nécessaire pour refill automatique
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            }));

        // ---------- CORS ----------
        services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazor", p =>
                    p.WithOrigins(
                        "https://localhost:7101",     // dev
                        "https://app.wallonie-en-poche.example" // prod
                     )
                     .AllowAnyHeader()
                     .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
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
        services.AddSignalR(o =>
        {
            o.EnableDetailedErrors = false;                // less information leakage
            o.MaximumReceiveMessageSize = 64 * 1024;       // 64 KB by default
            o.HandshakeTimeout = TimeSpan.FromSeconds(5);
            o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            o.KeepAliveInterval = TimeSpan.FromSeconds(10);
            o.AddFilter<ThrottleHubFilter>();              // ✅ global anti-flood
        });

        //services.AddHubOptions<OutZenHub>(o => { o.MaximumReceiveMessageSize = 32 * 1024; });
        //services.AddHubOptions<GPTHub>(o => { o.MaximumReceiveMessageSize = 128 * 1024; }); // only if justified
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
        //        context.Response.Headers.Add("X-API-Copyright", "© 2025 POLLESSI / CitizenHackathon2025. Reproduction prohibited.");
        //        await next();
        //    });
        //}

        // ===== Pipeline =====
        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // ⬇️ Security headers (before static files)
        app.UseSecurityHeaders();

        app.UseStaticFiles();

        var enableSwagger = app.Configuration.GetValue<bool?>("Swagger:Enabled")
                   ?? app.Environment.IsDevelopment();

        // Middlewares custom
        app.UseExceptionMiddleware();
        app.UseAntiXssMiddleware();
        // app.UseSecurityHeaders(); // (already called above — avoids duplication)
        app.UseUserAgentFiltering();
        app.UseAuditLogging();

        app.UseRouting();

        // Metrics
        app.UseHttpMetrics();
        app.UseMetricServer("/metrics");

        // CORS
        app.UseCors("AllowBlazor");

        // Auth
        app.UseAuthentication();

        // Minimal middleware Anti-forgery (cookie issue on GET)
        app.Use(async (ctx, next) =>
        {
            if (HttpMethods.IsGet(ctx.Request.Method))
            {
                var af = ctx.RequestServices.GetRequiredService<IAntiforgery>();
                var tokens = af.GetAndStoreTokens(ctx);
                ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false, Secure = true, SameSite = SameSiteMode.None });
            }
            await next();
        });

        // Rate Limiter
        app.UseRateLimiter();

        // ⇩⇩⇩ ONLY applies this to /api (not /swagger, /, /static, etc.)
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
            b => b.UseMiddleware<OutZenTokenMiddleware>());

        app.UseAuthorization();

        // Routes API (with rate limiting by group)
        app.MapGroup("/api")
           .RequireRateLimiting("per-user")
           .MapControllers();

        // (If you have other controllers outside /api, uncomment the line below)
        // app.MapControllers();

        // ---------- Hubs ----------
        var hubs = app.MapGroup("/hubs").RequireAuthorization(); // ✅ all protected by default

        // Mapped Hubs (inherit from Authorize)
        hubs.MapHub<AISuggestionHub>(TourismeHubMethods.HubPath);
        hubs.MapHub<CrowdHub>(CrowdHubMethods.HubPath);
        hubs.MapHub<EventHub>(EventHubMethods.HubPath);
        hubs.MapHub<GPTHub>(GptInteractionHubMethods.HubPath);
        hubs.MapHub<NotificationHub>(NotificationHubMethods.HubPath);
        hubs.MapHub<OutZenHub>(OutZenHubMethods.HubPath, o =>
        {
            o.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents; // 🎯 specific OutZen
        });
        hubs.MapHub<PlaceHub>(PlaceHubMethods.HubPath);
        hubs.MapHub<SuggestionHub>(SuggestionHubMethods.HubPath);
        hubs.MapHub<TrafficHub>(TrafficConditionHubMethods.HubPath);
        hubs.MapHub<UpdateHub>(UpdateHubMethods.HubPath);
        hubs.MapHub<UserHub>(UserHubMethods.HubPath);
        hubs.MapHub<WeatherForecastHub>(WeatherForecastHubMethods.HubPath);

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

        // Swagger (according to your configuration)
        if (enableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "CitizenHackathon2025 API V1"); });

            if (!app.Environment.IsDevelopment())
                app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
                    b => b.Use(async (ctx, next) =>
                    {
                        var auth = ctx.Request.Headers.Authorization.ToString();
                        if (auth != "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:strong-pass")))
                        { ctx.Response.StatusCode = 401; ctx.Response.Headers["WWW-Authenticate"] = "Basic realm=\"docs\""; return; }
                        await next();
                    }));
        }

        app.MapFallbackToFile("index.html");

        app.Run();
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.