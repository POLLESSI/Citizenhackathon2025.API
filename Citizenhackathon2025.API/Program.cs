using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CitizenHackathon2025.API.Azure.Security.KeyVault;
using CitizenHackathon2025.API.BackgroundWorkers;
using CitizenHackathon2025.API.Extensions;
using CitizenHackathon2025.API.Hubs;
using CitizenHackathon2025.API.Hubs.Serilog.Sinks;
using CitizenHackathon2025.API.Middlewares;
using CitizenHackathon2025.API.Options;
using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.Behaviors;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Application.Models;
using CitizenHackathon2025.Application.Pipeline;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Abstractions;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Filters;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure;
using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;
using CitizenHackathon2025.Infrastructure.Extensions;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Adapters;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Services;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Services;
using CitizenHackathon2025.Infrastructure.Init;
using CitizenHackathon2025.Infrastructure.Options;
using CitizenHackathon2025.Infrastructure.Persistence;
using CitizenHackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.Infrastructure.Resilience;
using CitizenHackathon2025.Infrastructure.Security;
using CitizenHackathon2025.Infrastructure.Services;
using CitizenHackathon2025.Infrastructure.Services.Monitoring;
using CitizenHackathon2025.Infrastructure.UseCases;
using CitizenHackathon2025.Shared.Interfaces;
using CitizenHackathon2025.Shared.Notifications;
using CitizenHackathon2025.Shared.Options;
using CitizenHackathon2025.Shared.Services;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Dapper;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_BROWSERLINK_ENABLED", "false");

        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        var services = builder.Services;
        var env = builder.Environment;

        Console.WriteLine($"ENV = {env.EnvironmentName}");
        Console.WriteLine("TrafficHmacKeyBase64 = " + (configuration["Security:TrafficHmacKeyBase64"] ?? "<null>"));

        ConfigureSerilog(builder);
        ConfigureMapster();
        ConfigureSqlAlwaysEncrypted();
        ConfigureOpenTelemetry(services);
        ConfigureDataProtection(services, env);
        ConfigureOptions(services, configuration);
        ConfigureSecrets(services, configuration);
        ConfigureDatabase(services, configuration);
        ConfigureAuthentication(services, configuration, env);
        ConfigureAuthorization(services);
        ConfigureAntiforgery(services);
        ConfigureRateLimiting(services);
        ConfigureCors(services);
        ConfigureControllers(services);
        ConfigureSignalR(services);
        ConfigureSwagger(services);
        ConfigureHttpClients(services, configuration);
        ConfigureRepositories(services);
        ConfigureApplicationServices(services);
        ConfigureHostedServices(services);
        ConfigureMediatR(services);

#if DEBUG
        services
            .AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Admin", Policies.AdminPolicy);
            })
            .AddRazorRuntimeCompilation();
#else
        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/Admin", Policies.AdminPolicy);
        });
#endif

        services.AddEndpointsApiExplorer();
        services.AddInfrastructure();
        services.AddInfrastructureServices();
        services.AddOutZenServices();

        var app = builder.Build();

        await RunStartupChecksAsync(app);

        ConfigurePipeline(app);

        app.Run();
    }

    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        AzureEventHub.ConfigureSerilog(builder.Configuration);

        Log.Logger = new LoggerConfiguration()
            .Destructure.ByTransforming<LogsDTO>(x => new
            {
                x.Id,
                Sensitive = "***"
            })
            .CreateLogger();

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
                        (e.Properties.TryGetValue("CorrelationId", out var cid)
                            ? $"{e.Level}-{cid}"
                            : e.Level.ToString())
                };

                lc.WriteTo.AzureEventHub(opt, new CompactJsonFormatter());
            }
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
    }

    private static void ConfigureMapster()
    {
        TypeAdapterConfig.GlobalSettings.Scan(AppDomain.CurrentDomain.GetAssemblies());
    }

    private static void ConfigureSqlAlwaysEncrypted()
    {
        var akvProvider = new SqlColumnEncryptionAzureKeyVaultProvider(new DefaultAzureCredential());

        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
            new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
            {
                { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, akvProvider }
            });
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("CitizenHackathon2025.API"))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://localhost:4317")))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());
    }

    private static void ConfigureDataProtection(IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(env.ContentRootPath, "dpkeys")))
            .SetApplicationName("CitizenHackathon2025");
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
        services.Configure<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>(configuration.GetSection("OpenWeather"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SessionJanitorOptions>(configuration.GetSection("Sessions:Janitor"));
        services.Configure<TrafficApiOptions>(configuration.GetSection("TrafficApi"));
        services.Configure<CitizenHackathon2025.API.Options.AntennaCleanupOptions>(configuration.GetSection("AntennaCleanup"));
        services.Configure<AntennaArchiveRetentionOptions>(configuration.GetSection("AntennaArchiveRetention"));
        services.Configure<TrafficHmacOptions>(configuration.GetSection("Security"));
        services.Configure<MorningCrowdAdvisoryHostedService.AdvisoryOptions>(configuration.GetSection("CrowdAdvisory"));
        services.Configure<DeviceHasherOptions>(configuration.GetSection("DeviceHasher"));

        services.AddOptions<CitizenHackathon2025.Shared.Options.SecurityOptions>()
            .Bind(configuration.GetSection("Security"))
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.PromptHashPepper),
                "Missing Security:PromptHashPepper in configuration.")
            .ValidateOnStart();

        services.AddOptions<CrowdInfoArchiverOptions>("CrowdInfo")
            .Bind(configuration.GetSection("Archivers:CrowdInfo"))
            .ValidateOnStart();

        services.AddOptions<GptInteractionArchiverOptions>("GptInteractions")
            .Bind(configuration.GetSection("Archivers:GptInteractions"))
            .ValidateOnStart();

        services.AddOptions<TrafficConditionArchiverOptions>("Traffic")
            .Bind(configuration.GetSection("Archivers:Traffic"))
            .ValidateOnStart();

        services.AddOptions<WeatherForecastArchiverOptions>("Weather")
            .Bind(configuration.GetSection("Archivers:Weather"))
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<TrafficHmacOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opt.TrafficHmacKeyBase64))
                throw new InvalidOperationException("Missing Security:TrafficHmacKeyBase64");

            try
            {
                return Convert.FromBase64String(opt.TrafficHmacKeyBase64.Trim());
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Invalid Base64 in Security:TrafficHmacKeyBase64.",
                    ex);
            }
        });
    }

    private static void ConfigureSecrets(IServiceCollection services, IConfiguration configuration)
    {
        var kvUri = configuration["KeyVault:VaultUri"];

        services.AddSingleton<IMemoryCache, MemoryCache>();

        if (!string.IsNullOrWhiteSpace(kvUri))
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("KeyVault");
                logger.LogInformation("KeyVault configured at {VaultUri}", kvUri);

                return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
            });

            services.AddSingleton<ISecrets, Secrets>();
        }
        else
        {
            services.AddSingleton<ISecrets>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();

                var fake = new CitizenHackathon2025.API.Azure.Security.KeyVault.Secrets(
                    new SecretClient(new Uri("https://example.vault.azure.net/"), new DefaultAzureCredential()),
                    sp.GetRequiredService<IMemoryCache>(),
                    sp.GetRequiredService<ILogger<CitizenHackathon2025.API.Azure.Security.KeyVault.Secrets>>(),
                    cacheTtl: TimeSpan.FromSeconds(30)
                );

                var pepper = cfg["DeviceHasher:PepperBase64"];
                if (!string.IsNullOrEmpty(pepper))
                {
                    sp.GetRequiredService<IMemoryCache>()
                        .Set("kv:device-pepper", pepper, TimeSpan.FromHours(1));
                }

                return fake;
            });
        }
    }

    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DbConnectionFactory>();
        services.AddScoped<IDbConnection>(_ => new SqlConnection(configuration.GetConnectionString("default")));
        services.AddScoped<DatabaseService>();

        SqlMapper.AddTypeHandler(new RoleTypeHandler());
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var securityEnabled = configuration.GetValue("Security:Enabled", true);
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

        if (env.IsDevelopment() && !securityEnabled)
        {
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Dev";
                o.DefaultChallengeScheme = "Dev";
            })
            .AddScheme<AuthenticationSchemeOptions, CitizenHackathon2025.API.Security.DevAuthHandler>("Dev", _ => { });

            return;
        }

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
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = hasIssuer,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = hasAudience,
                    ValidAudience = jwt.Audience,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var path = ctx.HttpContext.Request.Path;
                        var fromQuery = ctx.Request.Query["access_token"];
                        var fromCookie = ctx.Request.Cookies.TryGetValue(Cookies.JwtTokenName, out var cookie)
                            ? cookie
                            : null;

                        if (path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(fromQuery))
                        {
                            ctx.Token = fromQuery;
                        }
                        else if (!string.IsNullOrWhiteSpace(fromCookie))
                        {
                            ctx.Token = fromCookie;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(o =>
        {
            o.AddPolicy(Policies.AdminPolicy, p => p.RequireRole(Roles.Admin));
            o.AddPolicy(Policies.ModoPolicy, p => p.RequireRole(Roles.Admin, Roles.Modo));
            o.AddPolicy(Policies.UserPolicy, p => p.RequireRole(Roles.Admin, Roles.Modo, Roles.User));
            o.AddPolicy(Policies.GuestPolicy, p => p.RequireRole(Roles.Guest));
        });
    }

    private static void ConfigureAntiforgery(IServiceCollection services)
    {
        services.AddAntiforgery(o =>
        {
            o.Cookie.Name = "XSRF-TOKEN";
            o.Cookie.HttpOnly = false;
            o.HeaderName = "X-XSRF-TOKEN";
        });
    }

    private static void ConfigureRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(_ => _
            .AddPolicy("per-user", http =>
            {
                var userId = http.User?.Identity?.Name
                             ?? http.Connection.RemoteIpAddress?.ToString()
                             ?? "anon";

                return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            }));
    }

    private static void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazor", p =>
                p.WithOrigins(
                    "https://localhost:7101",
                    "http://localhost:7101",
                    "https://localhost:7254",
                    "http://localhost:7254",
                    "https://app.wallonie-en-poche.example"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
    }

    private static void ConfigureControllers(IServiceCollection services)
    {
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
                        .Where(e => e.Value?.Errors.Count > 0)
                        .Select(e => new
                        {
                            Field = e.Key,
                            Errors = e.Value!.Errors.Select(err => err.ErrorMessage)
                        });

                    return new BadRequestObjectResult(new
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                };
            });
    }

    private static void ConfigureSignalR(IServiceCollection services)
    {
        services.AddSignalR(o =>
        {
            o.EnableDetailedErrors = true;
            o.MaximumReceiveMessageSize = 64 * 1024;
            o.HandshakeTimeout = TimeSpan.FromSeconds(5);
            o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            o.KeepAliveInterval = TimeSpan.FromSeconds(10);
            o.AddFilter<ThrottleHubFilter>();
        });
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
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
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CitizenHackathon2025",
                Version = "v1"
            });

            c.CustomSchemaIds(t => t.FullName!.Replace("+", "."));
        });
    }

    private static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ResiliencePipelines>(sp => ResiliencePipelinesFactory.Create(sp));

        services.AddHttpClient<IMistralAIService, MistralAIService>((sp, client) =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:11434/");
            client.Timeout = TimeSpan.FromSeconds(300);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        })
        .AddHttpMessageHandler(sp =>
        {
            var pipelines = sp.GetRequiredService<ResiliencePipelines>();
            return new ResilienceHandler(pipelines.Ollama);
        });

        services.AddHttpClient<ITrafficApiService, TrafficAPIService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["TrafficApi:BaseUrl"];

            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        });

        services.AddHttpClient<IOdwbTrafficApiService, OdwbTrafficApiService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var endpoint = cfg["ODWB:Endpoint"];

            if (!string.IsNullOrWhiteSpace(endpoint))
                client.BaseAddress = new Uri(endpoint, UriKind.Absolute);

            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        })
        .AddHttpMessageHandler(sp =>
        {
            var pipelines = sp.GetRequiredService<ResiliencePipelines>();
            return new ResilienceHandler(pipelines.Ollama);
        });

        services.AddHttpClient("OpenWeatherRaw", (sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>>().Value;
            client.BaseAddress = new Uri((opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/') + "/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        });

        services.AddHttpClient<CitizenHackathon2025.Application.Interfaces.OpenWeather.IOpenWeatherService, CitizenHackathon2025.Infrastructure.Services.OpenWeatherService>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>>().Value;
            client.BaseAddress = new Uri((opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/') + "/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        });

        services.AddHttpClient<CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces.IOpenWeatherAlertsClient, CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.OpenWeatherAlertsClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl ?? "https://api.openweathermap.org");
            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        });

        services.AddHttpClient<CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces.IOpenWeatherCurrentClient, CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Clients.OpenWeatherCurrentClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl ?? "https://api.openweathermap.org");
            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        });

        services.AddHttpClient<IGptExternalService, CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI.OpenAIGptExternalService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var openAiKey = cfg["OpenAI:ApiKey"];

            client.BaseAddress = new Uri(cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com");

            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiKey);
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        })
        .AddHttpMessageHandler(sp =>
        {
            var pipelines = sp.GetRequiredService<ResiliencePipelines>();
            return new ResilienceHandler(pipelines.Ollama);
        });
    }

    private static void ConfigureRepositories(IServiceCollection services)
    {
        services.AddScoped<IAIRepository, AIRepository>();
        services.AddScoped<ICrowdInfoRepository, CrowdInfoRepository>();
        services.AddScoped<ICrowdInfoAntennaRepository, CrowdInfoAntennaRepository>();
        services.AddScoped<ICrowdInfoAntennaConnectionRepository, CrowdInfoAntennaConnectionRepository>();
        services.AddScoped<ICrowdCalendarRepository, CrowdCalendarRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IGptInteractionRepository, GptInteractionsRepository>();
        services.AddScoped<IGPTRepository, GptInteractionsRepository>();
        services.AddScoped<ILocalAiDataRepository, LocalAiDataRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISuggestionRepository, SuggestionRepository>();
        services.AddScoped<ITrafficConditionRepository, TrafficConditionRepository>();
        services.AddScoped<IUserMessageRepository, UserMessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
        services.AddScoped<IWeatherAlertRepository, WeatherAlertRepository>();
    }

    private static void ConfigureApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<INotifierAdmin, NotifierAdmin>();
        services.AddSingleton<ITimeZoneConverter, DefaultTimeZoneConverter>();
        services.AddSingleton<TokenGenerator>();
        services.AddSingleton<IDeviceHasher, DeviceHasher>();

        services.AddSingleton<ICspViolationStore, CspViolationStore>();
        services.AddMemoryCache();
        services.AddScoped<MemoryCacheService>();

        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IAggregateSuggestionService, AstroIAService>();
        services.AddScoped<ICrowdInfoService, CrowdInfoService>();
        services.AddScoped<CrowdInfoService>();
        services.AddScoped<ICrowdAdvisoryService, CrowdAdvisoryService>();
        services.AddScoped<ICrowdInfoAntennaService, CrowdInfoAntennaService>();
        services.AddScoped<ICrowdInfoAntennaConnectionService, CrowdInfoAntennaConnectionService>();
        services.AddScoped<CitizenSuggestionService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventReadService, EventReadService>();
        services.AddScoped<IGeoService, GeoService>();
        services.AddScoped<IGPTService, GPTService>();
        services.AddScoped<IMessageCorrelationService, MessageCorrelationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<IPasswordHasher, Sha512PasswordHasher>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<ITrafficConditionService, TrafficConditionService>();
        services.AddScoped<ITrafficIngestionService, TrafficIngestionService>();
        services.AddScoped<ITrafficOdwbIngestionService, TrafficOdwbIngestionService>();
        services.AddScoped<ILocalAiContextService, LocalAiContextService>();
        services.AddScoped<IUserMessageService, UserMessageService>();
        services.AddScoped<IWallonieEnPocheSourceClient, FakeWallonieEnPocheSourceClient>();
        services.AddScoped<IWallonieEnPocheSyncRepository, WallonieEnPocheSyncRepository>();
        services.AddScoped<IWallonieEnPocheSyncService, WallonieEnPocheSyncService>();
        services.AddScoped<IWeatherAlertsIngestionService, WeatherAlertsIngestionService>();
        services.AddScoped<IWeatherForecastAppService, WeatherForecastAppService>();
        services.AddScoped<IWeatherForecastBroadcaster, WeatherForecastBroadcaster>();
        services.AddScoped<IUserHubService, UserHubService>();
        services.AddScoped<IWeatherForecastService, WeatherForecastService>();
        services.AddScoped<IWeatherHubService, CitizenHackathon2025.Hubs.Services.WeatherHubService>();
        services.AddScoped<IHubNotifier, CitizenHackathon2025.Hubs.Hubs.SignalRNotifier>();
        services.AddScoped<NotificationService>();
        services.AddScoped<OpenAiSuggestionService>();
        services.AddScoped<TrafficConditionService>();
        services.AddScoped<WeatherSuggestionOrchestrator>();
        services.AddScoped<MistralContextBuilder>();

        services.AddScoped<
            CitizenHackathon2025.Application.Interfaces.IUserService,
            CitizenHackathon2025.Infrastructure.Services.UserService>();

        services.AddScoped<
            CitizenHackathon2025.Domain.Interfaces.IUserRepository,
            CitizenHackathon2025.Infrastructure.Repositories.UserRepository>();

        services.AddScoped<
            CitizenHackathon2025.Application.Interfaces.IUserHubService,
            CitizenHackathon2025.Infrastructure.Services.UserHubService>();

        services.AddScoped<IOpenWeatherIngestionService, OpenWeatherIngestionService>();
    }

    private static void ConfigureHostedServices(IServiceCollection services)
    {
        services.AddHostedService<CrowdInfoArchiverService>();
        services.AddHostedService<AntennaConnectionCleanupWorker>();
        services.AddHostedService<GptInteractionArchiverService>();
        services.AddHostedService<TrafficConditionArchiverService>();
        services.AddHostedService<WeatherForecastArchiverService>();
        services.AddHostedService<AntennaArchivePurgeWorker>();
        services.AddHostedService<MorningCrowdAdvisoryHostedService>();
        services.AddHostedService<EventArchiverService>();
        services.AddHostedService<OdwbTrafficCollector>();
        services.AddHostedService<WallonieEnPocheSyncWorker>();
        services.AddHostedService<WeatherService>();
        services.AddHostedService<SessionJanitor>();
    }

    private static void ConfigureMediatR(IServiceCollection services)
    {
        services.AddMediatR(typeof(GetLatestForecastQuery).Assembly);
        services.AddMediatR(typeof(GetSuggestionsByUserQuery).Assembly);
        services.AddMediatR(typeof(CitizenHackathon2025.Application.CQRS.Queries.Handlers.GetLatestTrafficConditionQueryHandler).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResilienceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static async Task RunStartupChecksAsync(WebApplication app)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Console.Error.WriteLine($"UNHANDLED: {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.Error.WriteLine($"UNOBSERVED: {e.Exception}");
            e.SetObserved();
        };

        using (var scope = app.Services.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<CitizenHackathon2025.Application.Interfaces.IUserService>();
            _ = scope.ServiceProvider.GetRequiredService<CitizenHackathon2025.Domain.Interfaces.IUserRepository>();
            _ = scope.ServiceProvider.GetRequiredService<CitizenHackathon2025.Application.Interfaces.IUserHubService>();
            _ = scope.ServiceProvider.GetRequiredService<CitizenSuggestionService>();
        }

        using (var scope = app.Services.CreateScope())
        {
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInit");
            var conn = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            await DbInit.RunOnceAsync(conn, env.ContentRootPath, log);
        }
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        var env = app.Environment;
        var enableSwagger = app.Configuration.GetValue<bool?>("Swagger:Enabled") ?? env.IsDevelopment();

        app.UseExceptionMiddleware();
        app.UseSecurityHeaders();

        if (!env.IsDevelopment())
        {
            app.UseHsts();
            app.UseUserAgentFiltering();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // Swagger Special CSP in DEV
        if (env.IsDevelopment())
        {
            app.UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase),
                branch =>
                {
                    branch.Use(async (ctx, next) =>
                    {
                        ctx.Response.OnStarting(() =>
                        {
                            var h = ctx.Response.Headers;

                            h.Remove("Content-Security-Policy");
                            h["Content-Security-Policy"] =
                                "default-src 'self'; " +
                                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                                "style-src 'self' 'unsafe-inline'; " +
                                "img-src 'self' data:; " +
                                "font-src 'self' data:; " +
                                "connect-src 'self' https://localhost:7254 wss://localhost:7254 http://localhost:* ws://localhost:* wss://localhost:*; " +
                                "frame-ancestors 'none'; " +
                                "base-uri 'self'; " +
                                "form-action 'self';";

                            return Task.CompletedTask;
                        });

                        await next();
                    });
                });
        }

        app.UseHttpMetrics();

        if (env.IsDevelopment())
        {
            app.UseMetricServer("/metrics");

            app.MapGet("/_whoami", (HttpContext ctx) =>
            {
                var u = ctx.User;
                return Results.Json(new
                {
                    Authenticated = u.Identity?.IsAuthenticated,
                    Name = u.Identity?.Name,
                    Roles = u.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
                });
            }).RequireAuthorization();

            app.MapGet("/_whoami-user", (HttpContext ctx) =>
            {
                var u = ctx.User;
                return Results.Json(new
                {
                    Authenticated = u.Identity?.IsAuthenticated,
                    Name = u.Identity?.Name,
                    Roles = u.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
                });
            }).RequireAuthorization(Policies.UserPolicy);

            app.MapGet("/_diag/routes", (EndpointDataSource es) =>
                Results.Ok(es.Endpoints.Select(e => e.DisplayName)));

            app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.Ok()).AllowAnonymous();
        }

        app.UseCors("AllowBlazor");
        app.UseRateLimiter();

        app.Use(async (ctx, next) =>
        {
            if (HttpMethods.IsGet(ctx.Request.Method))
            {
                var af = ctx.RequestServices.GetRequiredService<IAntiforgery>();
                var tokens = af.GetAndStoreTokens(ctx);

                ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = !env.IsDevelopment(),
                    SameSite = SameSiteMode.Lax
                });
            }

            await next();
        });

        app.UseSessionHeartbeat();
        app.UseAuthentication();
        app.UseAuthorization();

        if (!env.IsDevelopment() && app.Configuration.GetValue("OutZen:RequireEventId", true))
        {
            app.UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments("/api/outzen", StringComparison.OrdinalIgnoreCase),
                branch => branch.UseMiddleware<OutZenTokenMiddleware>());
        }

        app.UseAuditLogging();
        app.UseSerilogRequestLogging();

        if (enableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CitizenHackathon2025 API V1");
            });
        }

        app.MapRazorPages();
        app.MapControllers();

        MapHubs(app);
        MapEndpoints(app);

        app.MapFallbackToFile("index.html");
    }
    private static void MapHubs(WebApplication app)
    {
        var hubs = app.MapGroup("/hubs");

        hubs.MapHub<WeatherForecastHub>(WeatherForecastHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<CrowdHub>(CrowdHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<CrowdCalendarHub>(CrowdCalendarHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<CrowdInfoAntennaHub>(CrowdInfoAntennaHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<CrowdInfoAntennaConnectionHub>(CrowdInfoAntennaConnectionHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<SuggestionHub>(SuggestionHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<TrafficHub>(TrafficConditionHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<GPTHub>(GptInteractionHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<MessageHub>(MessageHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<PlaceHub>(PlaceHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<UpdateHub>(UpdateHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<UserHub>(UserHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<EventHub>(EventHubMethods.HubPath);
        hubs.MapHub<NotificationHub>(CitizenHackathon2025.Contracts.Hubs.NotificationHubMethods.HubPath).RequireAuthorization();

        hubs.MapHub<OutZenHub>(OutZenHubMethods.HubPath, o =>
        {
            o.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
        }).RequireAuthorization();
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/auth/hub-token", (HttpContext http, TokenGenerator tokens) =>
        {
            if (http.User?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

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

        app.MapGet("/", () => "OK");
    }
}































































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.