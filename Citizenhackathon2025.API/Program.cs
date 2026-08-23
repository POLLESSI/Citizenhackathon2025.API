extern alias AzureIdentity;
using Azure.Security.KeyVault.Secrets;
using CitizenHackathon2025.API.Azure.Security.KeyVault;
using CitizenHackathon2025.API.BackgroundServices;
using CitizenHackathon2025.API.BackgroundWorkers;
using CitizenHackathon2025.API.Extensions;
using CitizenHackathon2025.API.Hubs;
using CitizenHackathon2025.API.Hubs.Serilog.Sinks;
using CitizenHackathon2025.API.Middlewares;
using CitizenHackathon2025.API.Options;
using CitizenHackathon2025.API.Services;
using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.Behaviors;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Intelligence.AlertFusion;
using CitizenHackathon2025.Application.Intelligence.AlertFusion.AlertFusion;
using CitizenHackathon2025.Application.Intelligence.CommandCenter;
using CitizenHackathon2025.Application.Intelligence.Decision;
using CitizenHackathon2025.Application.Intelligence.Digital;
using CitizenHackathon2025.Application.Intelligence.Prediction;
using CitizenHackathon2025.Application.Intelligence.Replay;
using CitizenHackathon2025.Application.Intelligence.RiskAssessment;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Application.Models;
using CitizenHackathon2025.Application.Options;
using CitizenHackathon2025.Application.Pipeline;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Enums.CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Abstractions;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Records;
using CitizenHackathon2025.EmergencyIntelligence.Services;
using CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert;
using CitizenHackathon2025.EmergencyIntelligence.Workers;
using CitizenHackathon2025.Hubs.Filters;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure;
using CitizenHackathon2025.Infrastructure.Background;
//using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;
using CitizenHackathon2025.Infrastructure.Extensions;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Adapters;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Services;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Services;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Wallonie.Antennas;
using CitizenHackathon2025.Infrastructure.ExternalProviders.Common;
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
using CitizenHackathon2025.Worker.Gpt;
using Dapper;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
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
using Polly.Extensions.Http;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Data;
using System.Net;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DefaultAzureCredential = AzureIdentity::Azure.Identity.DefaultAzureCredential;

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
        ConfigureHostedServices(services, configuration, env);
        var gptWorkerRegistered = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(GptWorker));

        Console.WriteLine(
            $"GptWorker registered: {gptWorkerRegistered}");
        ConfigureMediatR(services);
        ConfigureNoSql(services, configuration);

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

        var hasDbConnectionRegistration = services.Any(descriptor => descriptor.ServiceType == typeof(IDbConnection));

        Console.WriteLine($"IDbConnection registered: " + $"{hasDbConnectionRegistration}");

        try
        {
            Console.WriteLine("[BOOT 1/4] builder.Build() beginning.");

            var app = builder.Build();

            Console.WriteLine("[BOOT 2/4] builder.Build() completed.");

            await RunStartupChecksAsync(app);

            Console.WriteLine("[BOOT 3/4] startup checks completed.");

            ConfigurePipeline(app);

            Console.WriteLine("[BOOT 4/4] HTTP pipeline configured.");

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("========== FATAL STARTUP ERROR ==========");

            Console.Error.WriteLine(ex.ToString());

            Console.Error.WriteLine("=========================================");

            Log.Fatal(ex, "CitizenHackathon2025 API startup failed.");

            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        AzureEventHub.ConfigureSerilog(builder.Configuration);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Destructure.ByTransforming<LogsDTO>(x => new
            {
                x.Id,
                Sensitive = "***"
            })
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
              .MinimumLevel.Information()
              .Enrich.FromLogContext()
              .Enrich.WithProperty(
                  "App",
                  "CitizenHackathon2025.API")
              .WriteTo.Console();

            var cs =
                ctx.Configuration[
                    "EventHubs:ConnectionString"];

            if (IsUsableEventHubConnectionString(cs))
            {
                var opt =
                    new AzureEventHubOptions
                    {
                        ConnectionString = cs!,
                        EventHubName = ctx.Configuration["EventHubs:EventHubName"],
                        BatchSizeLimit = ctx.Configuration.GetValue("EventHubs:BatchSizeLimit", 100),
                        Period = TimeSpan.FromSeconds(ctx.Configuration.GetValue("EventHubs:PeriodSeconds", 2)),
                        PartitionKeyResolver = e => e.Properties.TryGetValue("CorrelationId", out var cid) ? $"{e.Level}-{cid}" : e.Level.ToString()
                    };

                lc.WriteTo.AzureEventHub(
                    opt,
                    new CompactJsonFormatter());
            }
            else
            {
                Console.WriteLine(
                    "Azure EventHub logging disabled: " +
                    "missing or invalid " +
                    "EventHubs:ConnectionString.");
            }
        });
    }

    private static void ConfigureNoSql(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoPersistence(configuration);
    }
    private static bool IsUsableEventHubConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Contains("NOUVELLE_CONNECTION_STRING", StringComparison.OrdinalIgnoreCase))
            return false;

        if (value.Contains("your-namespace", StringComparison.OrdinalIgnoreCase))
            return false;

        if (value.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!value.Contains("Endpoint=sb://", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!value.Contains("SharedAccessKeyName=", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!value.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
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
        services.Configure<BeAlertCapOptions>(configuration.GetSection(BeAlertCapOptions.SectionName));
        services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
        services.Configure<CitizenHackathon2025.Shared.Options.OpenWeatherOptions>(configuration.GetSection("OpenWeather"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SessionJanitorOptions>(configuration.GetSection("Sessions:Janitor"));
        services.Configure<TrafficApiOptions>(configuration.GetSection("ExternalProviders:ODWB"));
        services.Configure<BeAlertCapOptions>(configuration.GetSection(BeAlertCapOptions.SectionName));
        services.Configure<CitizenHackathon2025.API.Options.AntennaCleanupOptions>(configuration.GetSection("AntennaCleanup"));
        services.Configure<AntennaArchiveRetentionOptions>(configuration.GetSection("AntennaArchiveRetention"));
        services.AddHostedService<AntennaConnectionCleanupWorker>();
        services.Configure<AntennaCadastreOptions>(configuration.GetSection("AntennaCadastre"));
        services.Configure<TrafficHmacOptions>(configuration.GetSection("Security"));
        services.Configure<MorningCrowdAdvisoryHostedService.AdvisoryOptions>(configuration.GetSection("CrowdAdvisory"));
        services.Configure<DeviceHasherOptions>(configuration.GetSection("DeviceHasher"));
        services.Configure<PipelineOptions>(configuration.GetSection("Pipelines"));
        services.AddOptions<CriticalAlertRules>().Bind(configuration.GetSection("CriticalAlertRules"))

            .Validate(rules =>rules.RequiredDistinctReports >= 4,
                "CriticalAlertRules:" +
                "RequiredDistinctReports must be at least 4.")

            .Validate(rules => rules.WindowMinutes is >= 1 and <= 30,
                "CriticalAlertRules:" +
                "WindowMinutes must be between 1 and 30.")

            .Validate(rules => rules.AlertDurationMinutes is >= 1 and <= 60,
                "CriticalAlertRules:" +
                "AlertDurationMinutes must be between 1 and 60.")

            .ValidateOnStart();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

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
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var opt = sp.GetRequiredService<IOptions<TrafficHmacOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opt.TrafficHmacKeyBase64))
            {
                if (env.IsDevelopment())
                {
                    var devKey = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

                    Console.WriteLine("DEV WARNING: Security:TrafficHmacKeyBase64 missing. Temporary in-memory key generated.");

                    return Convert.FromBase64String(devKey);
                }

                throw new InvalidOperationException("Missing Security:TrafficHmacKeyBase64");
            }

            try
            {
                return Convert.FromBase64String(opt.TrafficHmacKeyBase64.Trim());
            }
            catch (FormatException ex)
            {
                if (env.IsDevelopment())
                {
                    var devKey = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

                    Console.WriteLine("DEV WARNING: Invalid Security:TrafficHmacKeyBase64. Temporary in-memory key generated.");

                    return Convert.FromBase64String(devKey);
                }

                throw new InvalidOperationException(
                    "Invalid Base64 in Security:TrafficHmacKeyBase64.",
                    ex);
            }
        });
        services.AddSingleton(TimeProvider.System);
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
        var connectionString = configuration.GetConnectionString("default") ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQL connection string missing. Expected " + "'ConnectionStrings:default' or " + "'ConnectionStrings:DefaultConnection'.");
        }

        services.AddSingleton<DbConnectionFactory>();
        services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));
        services.AddScoped<DatabaseService>();

        SqlMapper.AddTypeHandler(new RoleTypeHandler());

        Console.WriteLine("SQL connection configured: True");
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
                        var fromCookie = ctx.Request.Cookies.TryGetValue(Cookies.JwtTokenName, out var cookie) ? cookie : null;

                        if (path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(fromQuery))
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
            o.AddPolicy(Policies.ModoPolicy, p => p.RequireRole(Roles.Admin, Roles.Moderator));
            o.AddPolicy(Policies.UserPolicy, p => p.RequireRole(Roles.Admin, Roles.Moderator, Roles.User));
            o.AddPolicy(Policies.GuestPolicy, p => p.RequireRole(Roles.Guest));
            o.AddPolicy("AdminOrModo", p => p.RequireRole(Roles.Admin, Roles.Moderator));
            o.AddPolicy("CrowdSafetyPolicy", p => p.RequireRole(Roles.Admin, Roles.LawEnforcement));
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
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("per-user", http =>
            {
                var key = http.User?.Identity?.Name
                          ?? http.Connection.RemoteIpAddress?.ToString()
                          ?? "anon";

                return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.AddPolicy("global", http =>
            {
                var key = http.User?.Identity?.Name
                          ?? http.Connection.RemoteIpAddress?.ToString()
                          ?? "anon";

                return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0
                });
            });

            options.AddPolicy("login", http =>
            {
                var key = http.Connection.RemoteIpAddress?.ToString() ?? "anon";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy("gpt", http =>
            {
                var key = http.User?.Identity?.Name
                          ?? http.Connection.RemoteIpAddress?.ToString()
                          ?? "anon";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy("external-provider", http =>
            {
                var key = http.User?.Identity?.Name
                          ?? http.Connection.RemoteIpAddress?.ToString()
                          ?? "anon";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy("signalr", http =>
            {
                var key = http.Connection.RemoteIpAddress?.ToString() ?? "anon";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });
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
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;

            // More comfortable if you have slightly larger DTOs
            options.MaximumReceiveMessageSize = 256 * 1024;

            // 5 seconds is a bit too tight in dev/local
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);

            // Very important: avoids unnecessary client reconnections
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(90);

            // eepAlive is consistent but not too aggressive
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            options.AddFilter<ThrottleHubFilter>();
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

        services.AddHttpClient<IAntennaCadastreImportService, AntennaCadastreImportService>();

        services.AddHttpClient<IGenerativeAiService, OllamaGenerativeAiService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var apiUrl = cfg["MistralAI:ApiUrl"] ?? "http://localhost:11434/";

            client.BaseAddress = new Uri(apiUrl.EndsWith("/api/chat", StringComparison.OrdinalIgnoreCase)
                ? apiUrl.Replace("/api/chat", "/")
                : apiUrl.TrimEnd('/') + "/");

            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");
        });

        services.AddHttpClient<IMistralAIService, MistralAIService>((sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<MistralAIService>>();
            var baseUrl = configuration["MistralAI:ApiBaseUrl"] ?? "http://127.0.0.1:11434/";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            /*
             * No HttpClient timeout.
             * MistralAIService handles it itself
             * maximum generation time.
             */
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025-OutZen/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            logger.LogWarning("[MISTRAL HTTP CONFIG] " + "BaseAddress={BaseAddress}; " + "HttpClientTimeout={HttpClientTimeout}; " + "PollyHandler=False", client.BaseAddress, client.Timeout);
        });

        services.AddHttpClient<IBeAlertCapSource, BeAlertCapSource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BeAlertCapOptions>>().Value;

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025-OutZen-BEAlert/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        });

        services.AddTransient<IEmergencyAlertSource>(sp => sp.GetRequiredService<IBeAlertCapSource>());

        services.AddHttpClient<INationalCrisisCenterAlertSource, NationalCrisisCenterAlertSource>();

        services.AddTransient<IEmergencyAlertSource>(sp => sp.GetRequiredService<INationalCrisisCenterAlertSource>());

        services.AddHttpClient<ITrafficApiService, TrafficAPIService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();

            var baseUrl = cfg["TrafficApi:BaseUrl"];

            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            client.DefaultRequestHeaders.Add("User-Agent", "CitizenHackathon2025");
        });

        services.AddHttpClient("ODWB", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["ExternalProviders:ODWB:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("ExternalProviders:ODWB:BaseUrl is missing.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(
                cfg.GetValue<int?>("ExternalProviders:ODWB:TimeoutSeconds") ?? 8);

            client.DefaultRequestHeaders.UserAgent.ParseAdd("curl/8.20.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                SslOptions =
                {
                    EnabledSslProtocols = SslProtocols.Tls12
                },
                AllowAutoRedirect = true,
                AutomaticDecompression =
                    DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        });

        services.AddHttpClient<IOdwbTrafficApiService, OdwbTrafficApiService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["ExternalProviders:ODWB:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("ExternalProviders:ODWB:BaseUrl is missing.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(
                cfg.GetValue<int?>("ExternalProviders:ODWB:TimeoutSeconds") ?? 8);

            client.DefaultRequestHeaders.UserAgent.ParseAdd("curl/8.20.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                SslOptions =
                {
                    EnabledSslProtocols = SslProtocols.Tls12
                },
                AllowAutoRedirect = true,
                AutomaticDecompression =
                    DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
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

        services.AddHttpClient<IGptExternalService,CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI.OpenAIGptExternalService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();

            var openAiKey = cfg["OpenAI:ApiKey"];

            client.BaseAddress = new Uri(cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com");

            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue( "Bearer", openAiKey);
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "CitizenHackathon2025/1.0");
        })
        .AddHttpMessageHandler(sp =>
        {
            var pipelines = sp.GetRequiredService<ResiliencePipelines>();

            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OpenAIHttpClient");

            logger.LogCritical("[OPENAI HANDLER ACTIVE] Using pipelines.OpenAi.");

            // ✅ OpenAI uses its own policy.
            return new ResilienceHandler(pipelines.OpenAi);
        });

        services.AddHttpClient<IOpenWeatherService, OpenWeatherService>();

        var owKey = configuration["OpenWeather:ApiKey"];

        if (string.IsNullOrWhiteSpace(owKey))
        {
            Console.WriteLine("⚠️ OpenWeather:ApiKey is missing.");
        }
        else if (owKey.Contains("NOUVELLE", StringComparison.OrdinalIgnoreCase) ||
                 owKey.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("⚠️ OpenWeather:ApiKey is a placeholder.");
        }
        else
        {
            Console.WriteLine($"✅ OpenWeather:ApiKey loaded. Length={owKey.Length}");
        }

        services.AddHttpClient<WallonieAntennaCadastreClient>(client =>
        {
            client.BaseAddress = new Uri(
                "https://geoservices.wallonie.be/arcgis/rest/services/INDUSTRIES_SERVICES/ANTENNES/MapServer/0/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025-OutZen/1.0");
        });

        services.AddHttpClient<ILocalCrowdDecisionService, OllamaCrowdDecisionService>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();

            var baseUrl = cfg["Ollama:BaseUrl"] ?? cfg["MistralAI:ApiBaseUrl"] ?? "http://localhost:11434/";

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025-OutZen-CrowdDecision/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
    }

    //private static IHttpClientBuilder AddProtectedHttpClient<TClient, TImplementation>(
    //IServiceCollection services,
    //IConfiguration configuration,
    //string providerName)
    //where TClient : class
    //where TImplementation : class, TClient
    //{
    //    var section = configuration.GetSection($"ExternalProviders:{providerName}");
    //    var options = section.Get<ExternalProviderOptions>()
    //        ?? throw new InvalidOperationException($"Missing ExternalProviders:{providerName}");

    //    services.Configure<ExternalProviderOptions>(section);

    //    return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
    //    {
    //        client.BaseAddress = new Uri(options.BaseUrl);
    //        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    //        client.MaxResponseContentBufferSize = options.MaxPayloadBytes;
    //        client.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025-OutZen/1.0");
    //    })
    //    .ConfigurePrimaryHttpMessageHandler(() =>
    //    {
    //        return new SocketsHttpHandler
    //        {
    //            AllowAutoRedirect = false,
    //            MaxConnectionsPerServer = 20,
    //            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    //            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
    //        };
    //    });
    //}
    private static void ConfigureRepositories(IServiceCollection services)
    {
        services.AddScoped<IAIRepository, AIRepository>();
        services.AddScoped<ICriticalAlertVoteRepository, CriticalAlertVoteRepository>();
        services.AddScoped<ICrowdAlertVoteRepository, CrowdAlertVoteRepository>();
        services.AddScoped<ICrowdInfoRepository, CrowdInfoRepository>();
        services.AddScoped<ICrowdInfoAntennaRepository, CrowdInfoAntennaRepository>();
        services.AddScoped<ICrowdInfoAntennaConnectionRepository, CrowdInfoAntennaConnectionRepository>();
        services.AddScoped<ICrowdSafetyAlertRepository, CrowdSafetyAlertRepository>();
        services.AddScoped<ICrowdCalendarRepository, CrowdCalendarRepository>();
        services.AddScoped<IDisasterAlertRepository, DisasterAlertRepository>();
        services.AddScoped<IEmergencyAlertRepository, EmergencyAlertRepository>();
        services.AddScoped<IEmergencyAlertPublisher, SignalREmergencyAlertPublisher>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IGptInteractionRepository, GptInteractionsRepository>();
        services.AddScoped<ILocalAiDataRepository, LocalAiDataRepository>();
        services.AddScoped<IPasswordHasher<Users>,PasswordHasher<Users>>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IProfanityRepository, ProfanityRepository>();
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
        services.AddSingleton<OutZenDomainGuard>();
        services.AddSingleton<TokenGenerator>();
        services.AddSingleton<IDeviceHasher, DeviceHasher>();
        services.AddSingleton<IGptRequestRegistry, GptRequestRegistry>();
        services.AddSingleton<ICspViolationStore, CspViolationStore>();
        services.AddSingleton<EmergencyAlertHubBroadcaster>();
        services.AddSingleton<IEmergencyAlertNormalizer, BeAlertCapNormalizer>();

        services.AddMemoryCache();
        services.AddScoped<MemoryCacheService>();

        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IAggregateSuggestionService, AstroIAService>();
        services.AddScoped<IAntennaCadastreImportService, AntennaCadastreImportService>();
        services.AddScoped<IAntennaSimulationService, AntennaSimulationService>();
        services.AddScoped<IAntennaZoneSimulationService, AntennaZoneSimulationService>();
        services.AddScoped<IAlertFusionEngine, AlertFusionEngine>();
        services.AddScoped<ICommandCenterService, CommandCenterService>();
        services.AddScoped<IReplayService, ReplayService>();
        services.AddScoped<IDigitalTwin, DigitalTwin>();
        services.AddScoped<IDecisionEngine, DecisionEngine>();
        services.AddScoped<OfficialEmergencyRiskContextService>();
        services.AddScoped<IPredictionEngine, PredictionEngine>();
        services.AddScoped<IRiskScoreCalculator, RiskScoreCalculator>();
        services.AddScoped<ICriticalAlertQuorumService, CriticalAlertQuorumService>();
        services.AddScoped<ICrowdInfoService, CrowdInfoService>();
        services.AddScoped<CrowdInfoService>();
        services.AddScoped<ICrowdAdvisoryService, CrowdAdvisoryService>();
        services.AddScoped<ICrowdInfoAntennaService, CrowdInfoAntennaService>();
        services.AddScoped<ICrowdInfoAntennaConnectionService, CrowdInfoAntennaConnectionService>();
        services.AddScoped<ICrowdSafetyAnalyzer, CrowdSafetyAnalyzer>();
        services.AddScoped<ICrowdSafetyDetectionService, CrowdSafetyDetectionService>();
        services.AddScoped<CitizenSuggestionService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventReadService, EventReadService>();
        services.AddScoped<IEmergencyAlertSyncOrchestrator, EmergencyAlertSyncOrchestrator>();
        services.AddScoped<IGeoService, GeoService>();
        services.AddSingleton<IGptBackgroundQueue, GptBackgroundQueue>();
        services.AddScoped<IGptQueuedRequestProcessor, GptOrchestrator>();
        services.AddScoped<IGptOrchestrator, GptOrchestrator>();
        services.AddScoped<ILanguagePromptBuilder, LanguagePromptBuilder>();
        services.AddScoped<ILocalAiContextService, LocalAiContextService>();
        services.AddScoped<IPlaceNameResolver, PlaceNameResolver>();
        services.AddScoped<IMessageCorrelationService, MessageCorrelationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ILegacyPasswordHasher, Sha512PasswordHasher>();
        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<IPredictionEngine, PredictionEngine>();
        services.AddScoped<IProfanityService, ProfanityService>();
        services.AddScoped<IProfanityAdminService, ProfanityAdminService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IReplayService, ReplayService>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<ITrafficConditionNormalizer, TrafficConditionNormalizer>();
        services.AddScoped<ITrafficConditionService, TrafficConditionService>();
        services.AddScoped<ITrafficIngestionService, TrafficIngestionService>();
        services.AddScoped<ITrafficOdwbIngestionService, TrafficOdwbIngestionService>();

        services.AddScoped<ITrafficProviderMapper<PerexTrafficRaw>, PerexTrafficMapper>();
        services.AddScoped<ITrafficProviderMapper<WazeTrafficRaw>, WazeTrafficMapper>();
        services.AddScoped<ITrafficProviderMapper<HereTrafficRaw>, HereTrafficMapper>();
        services.AddScoped<ITrafficProviderMapper<TomTomTrafficRaw>, TomTomTrafficMapper>();
        services.AddScoped<ITrafficProviderMapper<ManualTrafficRaw>, ManualTrafficMapper>();
        services.AddScoped<ITrafficProviderMapper<SignalRTrafficRaw>, SignalRTrafficMapper>();

        services.AddScoped<IUiTextLocalizer, UiTextLocalizer>();
        services.AddScoped<IUserMessageService, UserMessageService>();
        services.AddScoped<IUserSessionService, CitizenHackathon2025.Infrastructure.Services.UserSessionService>();
        services.AddScoped<IWallonieEnPocheSourceClient, FakeWallonieEnPocheSourceClient>();
        services.AddScoped<IWallonieEnPocheSyncRepository, WallonieEnPocheSyncRepository>();
        services.AddScoped<IWallonieEnPocheSyncService, WallonieEnPocheSyncService>();
        services.AddScoped<IWalloonNormalizer, WalloonNormalizer>();
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

    private static void ConfigureHostedServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        services.AddHostedService<CrowdSafetyAlertDetectorHostedService>();
        services.AddHostedService<EmergencyAlertSyncWorker>();
        services.AddHostedService<GptWorker>();

        if (!env.IsDevelopment())
        {
            services.AddHostedService<CrowdInfoArchiverService>();
            services.AddHostedService<AntennaConnectionCleanupWorker>();
            services.AddHostedService<AntennaArchivePurgeWorker>();
            services.AddHostedService<GptInteractionArchiverService>();
            services.AddHostedService<TrafficConditionArchiverService>();
            services.AddHostedService<WeatherForecastArchiverService>();
            services.AddHostedService<ExpiredDataArchiverHostedService>();
            services.AddHostedService<CrowdInfoAntennaCollectorHostedService>();
            services.AddHostedService<WallonieAntennaCadastreSyncHostedService>();
            services.AddHostedService<WallonieEnPocheSyncHostedService>();
        }

        services.AddHostedService<MorningCrowdAdvisoryHostedService>();
        services.AddHostedService<EventArchiverService>();
        services.AddHostedService<OdwbTrafficCollector>();
        services.AddHostedService<SessionJanitor>();
        services.AddHostedService<WallonieEnPocheSyncWorker>();
        services.AddHostedService<WeatherForecastCleanupHostedService>();
        services.AddHostedService<WeatherForecastCollectorHostedService>();
        services.AddHostedService<TrafficConditionCollectorHostedService>();
    }



    private static void ConfigureMediatR(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(GetLatestForecastQuery).Assembly,
                typeof(GetSuggestionsByUserQuery).Assembly,
                typeof(CitizenHackathon2025.Application.CQRS.Queries.Handlers.GetLatestTrafficConditionQueryHandler).Assembly
            );
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResilienceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static async Task RunStartupChecksAsync(WebApplication app)
    {
        Console.WriteLine("[STARTUP 1/6] Startup checks beginning.");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.Error.WriteLine($"[UNHANDLED] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.Error.WriteLine($"[UNOBSERVED] {e.Exception}");
            e.SetObserved();
        };

        Console.WriteLine("[STARTUP 2/6] Resolving critical services.");

        using (var scope = app.Services.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<CitizenHackathon2025.Application.Interfaces.IUserService>();
            _ = scope.ServiceProvider.GetRequiredService<CitizenHackathon2025.Domain.Interfaces.IUserRepository>();
            _ = scope.ServiceProvider.GetRequiredService<CitizenHackathon2025.Application.Interfaces.IUserHubService>();
            _ = scope.ServiceProvider.GetRequiredService<CitizenSuggestionService>();
        }

        Console.WriteLine("[STARTUP 3/6] Critical services resolved.");

        using (var scope = app.Services.CreateScope())
        {
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInit");
            var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            Console.WriteLine("[STARTUP 4/6] Opening SQL connection.");

            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[STARTUP SQL OPEN ERROR]");
                Console.Error.WriteLine(ex.ToString());

                throw;
            }

            Console.WriteLine("[STARTUP 5/6] SQL opened; DbInit beginning.");

            try
            {
                await DbInit.RunOnceAsync(connection, environment.ContentRootPath, logger).WaitAsync(TimeSpan.FromSeconds(60));
            }
            catch (TimeoutException ex)
            {
                Console.Error.WriteLine("[STARTUP DBINIT TIMEOUT]");
                Console.Error.WriteLine(ex.ToString());

                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[STARTUP DBINIT ERROR]");
                Console.Error.WriteLine(ex.ToString());

                throw;
            }
        }

        Console.WriteLine("[STARTUP 6/6] Startup checks completed.");
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

        app.UseAuthentication();
        app.UseSessionHeartbeat();
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
        hubs.MapHub<CrowdSafetyHub>(CrowdSafetyHubMethods.HubPath).RequireAuthorization("CrowdSafetyPolicy");
        hubs.MapHub<EmergencyAlertHub>(EmergencyAlertHubMethods.HubPath).RequireAuthorization().RequireRateLimiting("signalr");
        hubs.MapHub<SuggestionHub>(SuggestionHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<TrafficHub>(TrafficConditionHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<GPTHub>(GptInteractionHubMethods.HubPath, options =>
        {
            options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
        })
        .RequireAuthorization()
        .RequireRateLimiting("signalr");
        hubs.MapHub<MessageHub>(MessageHubMethods.HubPath).RequireAuthorization();
        hubs.MapHub<ModerationHub>(ModerationHubMethods.HubPath).RequireAuthorization(Policies.ModoPolicy);
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
        app.MapGet("/trafficcondition/latest",
            async (IMediator mediator, CancellationToken cancellationToken) =>
            {
                var list = await mediator.Send(new GetLatestTrafficConditionQuery(), cancellationToken);

                return list is null || list.Count == 0 ? Results.NotFound() : Results.Ok(list);
            });

        app.MapGet("/auth/hub-token", (HttpContext context, TokenGenerator tokenGenerator) =>
            {
                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    return Results.Unauthorized();
                }

                var token = tokenGenerator.GenerateTokenFromPrincipal(context.User,expiresInMinutes: 5);

                return Results.Ok(new
                {
                    token
                });
            })
            .RequireAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/_diag/emergency/source",
                async (INationalCrisisCenterAlertSource source, CancellationToken cancellationToken) =>
                {
                    var cursor = new EmergencyAlertCursor(
                            ETag: null,
                            LastModifiedUtc: null,
                            ContinuationToken: null,
                            LastSuccessfulFetchUtc: null);

                    var batch = await source.FetchAsync(cursor, cancellationToken);

                    return Results.Ok(new
                    {
                        SourceCode = source.SourceCode,
                        AlertCount = batch.Alerts.Count,
                        FetchedAtUtc = batch.FetchedAtUtc,
                        ETag = batch.ETag,
                        LastModifiedUtc = batch.LastModifiedUtc,
                        IsRemoteProviderConfigured = false
                    });
                })
            .AllowAnonymous();

            app.MapGet("/_diag/emergency/source/be-alert",
                async (IBeAlertCapSource source, CancellationToken ct) =>
                {
                    var cursor = new EmergencyAlertCursor(
                        ETag: null,
                        LastModifiedUtc: null,
                        ContinuationToken: null,
                        LastSuccessfulFetchUtc: null);

                    var batch = await source.FetchAsync(cursor,ct);

                    return Results.Ok(
                        new
                        {
                            source.SourceCode,
                            AlertCount = batch.Alerts.Count,
                            batch.FetchedAtUtc,
                            batch.ETag,
                            batch.LastModifiedUtc
                        }
                    );
                })
                .AllowAnonymous();

            app.MapPost("/_diag/emergency/sync",
                async (IEmergencyAlertSyncOrchestrator orchestrator, CancellationToken cancellationToken) =>
                {
                    await orchestrator.SynchronizeAllAsync(cancellationToken);

                    return Results.Ok(new
                    {
                        Success = true,

                        SynchronizedAtUtc = DateTimeOffset.UtcNow
                    });
                })
                .AllowAnonymous();

            app.MapPost("/_diag/emergency/persistence-test",
                async (IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
               {
                   var now = DateTimeOffset.UtcNow;
                   var externalId = $"OUTZEN-DIAG-{Guid.NewGuid():N}";


                   var alert = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                       {
                           Id = Guid.NewGuid(),
                           SourceCode = "OUTZEN-DIAG",
                           ExternalId = externalId,
                           ExternalReferenceId = null,
                           ReferencedExternalIds = Array.Empty<string>(),
                           CorrelationKey = $"OUTZEN-DIAG:{externalId}",

                           /*
                            * HazardType is not important for
                            * this repository test.
                            */
                           HazardType = default,
                           Severity = EmergencySeverity.Severe,
                           Urgency = EmergencyUrgency.Immediate,
                           Certainty = EmergencyCertainty.Observed,
                           Status = EmergencyAlertStatus.Active,
                           InformationKind = SafetyInformationKind.ActiveEmergency,
                           Headline = "OutZen emergency persistence test",
                           Description = "Diagnostic emergency alert used " + "to validate SQL persistence and SignalR.",
                           Instructions = "Diagnostic only.",
                           Language = "fr-BE",
                           SentAtUtc = now,
                           EffectiveFromUtc = now,
                           ExpiresAtUtc = now.AddMinutes(10),
                           LastUpdatedAtUtc = now,
                           Area = null,
                           RadiusMeters = null,
                           ProvinceCode = null,
                           MunicipalityCode = null,
                           OfficialInformationUri = null,

                           /*
                            * IMPORTANT:
                            * this is NOT an official alert.
                            */
                           IsOfficial = false,
                           IsMachineVerified = false,
                           IsActive = true,
                           PayloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(externalId))),
                           RawPayloadStorageKey = null,
                           CreatedAtUtc = now,
                           UpdatedAtUtc = now
                       };

                   var result = await repository.ApplyAsync(alert, cancellationToken);

                   if (result.Changed && result.IsActive)
                   {
                       await publisher.PublishUpsertedAsync(result.StoredAlert, cancellationToken);
                   }


                   return Results.Ok(
                       new
                       {
                           success = true,
                           result.StoredAlert.Id,
                           result.StoredAlert.SourceCode,
                           result.StoredAlert.ExternalId,
                           result.Changed,
                           result.IsActive,
                           RemovedCount = result.RemovedAlerts.Count
                       });
               })
            .AllowAnonymous();

            app.MapPost("/_diag/emergency/lifecycle-test",
                async (IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
                {
                    var now = DateTimeOffset.UtcNow;
                    var runId = Guid.NewGuid().ToString("N");
                    var sourceCode = "OUTZEN-LIFECYCLE-DIAG";
                    var externalIdA = $"LIFECYCLE-{runId}-A";
                    var externalIdB = $"LIFECYCLE-{runId}-B";
                    var externalIdC = $"LIFECYCLE-{runId}-C";

                    static string ComputeHash(string value)
                    {
                        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
                    }

                    static EmergencyAlertStatus ResolveCancelledStatus()
                    {
                        if (Enum.TryParse<EmergencyAlertStatus>("Cancelled", ignoreCase: true, out var cancelled))
                        {
                            return cancelled;
                        }

                        if (Enum.TryParse<EmergencyAlertStatus>("Canceled", ignoreCase: true, out var canceled))
                        {
                            return canceled;
                        }

                        throw new InvalidOperationException("EmergencyAlertStatus does not contain " + "'Cancelled' or 'Canceled'.");
                    }

                    // =====================================================
                    // A - INITIAL ACTIVE ALERT
                    // =====================================================

                    var alertA = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                        {
                            Id = Guid.NewGuid(),
                            SourceCode = sourceCode,
                            ExternalId = externalIdA,
                            ExternalReferenceId = null,
                            ReferencedExternalIds = Array.Empty<string>(),
                            CorrelationKey = $"OUTZEN-LIFECYCLE:{runId}",
                            HazardType = default,
                            Severity = EmergencySeverity.Severe,
                            Urgency = EmergencyUrgency.Immediate,
                            Certainty = EmergencyCertainty.Observed,
                            Status = EmergencyAlertStatus.Active,
                            InformationKind = SafetyInformationKind.ActiveEmergency,
                            Headline = "Lifecycle diagnostic A",
                            Description = "Initial diagnostic emergency alert.",
                            Instructions = "Diagnostic only.",
                            Language = "fr-BE",
                            SentAtUtc = now,
                            EffectiveFromUtc = now,
                            ExpiresAtUtc = now.AddMinutes(30),
                            LastUpdatedAtUtc = now,
                            Area = null,
                            RadiusMeters = null,
                            ProvinceCode = null,
                            MunicipalityCode = null,
                            OfficialInformationUri = null,
                            IsOfficial = false,
                            IsMachineVerified = false,
                            IsActive = true,
                            PayloadHash = ComputeHash($"A:{runId}"),
                            RawPayloadStorageKey = null,
                            CreatedAtUtc = now,
                            UpdatedAtUtc = now
                        };

                    var resultA = await repository.ApplyAsync(alertA, cancellationToken);

                    if (resultA.Changed && resultA.IsActive)
                    {
                        await publisher.PublishUpsertedAsync(resultA.StoredAlert, cancellationToken);
                    }

                    // =====================================================
                    // B - UPDATE THAT REFERENCES A
                    // =====================================================

                    var updateTime = DateTimeOffset.UtcNow;
                    var alertB = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                        {
                            Id = Guid.NewGuid(),
                            SourceCode = sourceCode,
                            ExternalId = externalIdB,
                            ExternalReferenceId = externalIdA,
                            ReferencedExternalIds = new[] {externalIdA},
                            CorrelationKey = $"OUTZEN-LIFECYCLE:{runId}",
                            HazardType = default,
                            Severity = EmergencySeverity.Severe,
                            Urgency = EmergencyUrgency.Immediate,
                            Certainty = EmergencyCertainty.Observed,
                            Status = EmergencyAlertStatus.Active,
                            InformationKind = SafetyInformationKind.ActiveEmergency,
                            Headline = "Lifecycle diagnostic B",
                            Description = "Updated diagnostic emergency alert.",
                            Instructions = "Updated diagnostic instruction.",
                            Language = "fr-BE",
                            SentAtUtc = updateTime,
                            EffectiveFromUtc = updateTime,
                            ExpiresAtUtc = updateTime.AddMinutes(30),
                            LastUpdatedAtUtc = updateTime,
                            Area = null,
                            RadiusMeters = null,
                            ProvinceCode = null,
                            MunicipalityCode = null,
                            OfficialInformationUri = null,
                            IsOfficial = false,
                            IsMachineVerified = false,
                            IsActive = true,
                            PayloadHash = ComputeHash($"B:{runId}"),
                            RawPayloadStorageKey = null,
                            CreatedAtUtc = updateTime,
                            UpdatedAtUtc = updateTime
                        };

                    var resultB = await repository.ApplyAsync(alertB, cancellationToken);

                    foreach (var removed in resultB.RemovedAlerts)
                    {
                        switch (removed.Reason)
                        {
                            case EmergencyAlertRemovalReason.Cancelled:
                                await publisher.PublishCancelledAsync(removed.Alert, cancellationToken);
                                break;

                            case EmergencyAlertRemovalReason.Expired:
                                await publisher.PublishExpiredAsync(removed.Alert, cancellationToken);
                                break;

                            case EmergencyAlertRemovalReason.Superseded:
                                /*
                                 * No "cancelled" semantic here.
                                 *
                                 * The incoming B upsert follows immediately and
                                 * the REST snapshot remains the source of truth.
                                 */
                                break;
                        }
                    }

                    if (resultB.Changed && resultB.IsActive)
                    {
                        await publisher.PublishUpsertedAsync(resultB.StoredAlert, cancellationToken);
                    }

                    // =====================================================
                    // C - CANCEL THAT REFERENCES B
                    // =====================================================

                    var cancelTime = DateTimeOffset.UtcNow;
                    var cancelledStatus = ResolveCancelledStatus();

                    var alertC = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                        {
                            Id = Guid.NewGuid(),
                            SourceCode = sourceCode,
                            ExternalId = externalIdC,
                            ExternalReferenceId = externalIdB,
                            ReferencedExternalIds = new[] {externalIdB},
                            CorrelationKey = $"OUTZEN-LIFECYCLE:{runId}",
                            HazardType = default,
                            Severity = EmergencySeverity.Severe,
                            Urgency = EmergencyUrgency.Immediate,
                            Certainty = EmergencyCertainty.Observed,
                            Status = cancelledStatus,
                            InformationKind = SafetyInformationKind.ActiveEmergency,
                            Headline = "Lifecycle diagnostic C",
                            Description = "Cancellation of diagnostic alert B.",
                            Instructions = "Diagnostic cancellation only.",
                            Language = "fr-BE",
                            SentAtUtc = cancelTime,
                            EffectiveFromUtc = cancelTime,
                            ExpiresAtUtc = null,
                            LastUpdatedAtUtc = cancelTime,
                            Area = null,
                            RadiusMeters = null,
                            ProvinceCode = null,
                            MunicipalityCode = null,
                            OfficialInformationUri = null,
                            IsOfficial = false,
                            IsMachineVerified = false,
                            IsActive = false,
                            PayloadHash = ComputeHash($"C:{runId}"),
                            RawPayloadStorageKey = null,
                            CreatedAtUtc = cancelTime,
                            UpdatedAtUtc = cancelTime
                        };

                    var resultC = await repository.ApplyAsync(alertC, cancellationToken);

                    foreach (var removed in resultC.RemovedAlerts)
                    {
                        await publisher.PublishCancelledAsync(removed.Alert, cancellationToken);
                    }

                    // =====================================================
                    // FINAL ACTIVE SNAPSHOT
                    // =====================================================

                    var activeAfterLifecycle = await repository.GetActiveAsync(cancellationToken);

                    var activeForThisRun = activeAfterLifecycle
                            .Where(x => string.Equals(x.SourceCode, sourceCode, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(x.CorrelationKey, $"OUTZEN-LIFECYCLE:{runId}", StringComparison.Ordinal))
                            .Select(x => new {x.Id, x.ExternalId, x.Status, x.IsActive})
                            .ToArray();

                    return Results.Ok(
                        new
                        {
                            success = true, runId, sourceCode,

                            alert =
                                new
                                {
                                    resultA.StoredAlert.Id,
                                    ExternalId = externalIdA,
                                    resultA.Changed,
                                    resultA.IsActive,
                                    RemovedCount = resultA.RemovedAlerts.Count
                                },

                            update =
                                new
                                {
                                    resultB.StoredAlert.Id,
                                    ExternalId = externalIdB,
                                    resultB.Changed,
                                    resultB.IsActive,
                                    RemovedCount = resultB.RemovedAlerts.Count
                                },

                            cancel =
                                new
                                {
                                    resultC.StoredAlert.Id,
                                    ExternalId = externalIdC,
                                    resultC.Changed,
                                    resultC.IsActive,
                                    RemovedCount = resultC.RemovedAlerts.Count
                                },

                            activeForThisRun
                        });
                })
                .AllowAnonymous();

            app.MapPost("/_diag/emergency/hub-test",
                async (EmergencyAlertHubBroadcaster broadcaster) =>
                {
                    var alert = new EmergencyAlertSignalRDTO
                    {
                        Id = Guid.NewGuid(),
                        SourceCode = "BE-NCCN",
                        ExternalId = $"TEST-{Guid.NewGuid():N}",
                        HazardType = EmergencyHazardType.Flood,
                        Severity = EmergencySeverity.Severe,
                        Urgency = EmergencyUrgency.Immediate,
                        Certainty = EmergencyCertainty.Observed,
                        Status = EmergencyAlertStatus.Active,
                        InformationKind = SafetyInformationKind.ActiveEmergency,
                        Headline = "TEST OutZen Emergency Intelligence",
                        Description = "Alerte de diagnostic SignalR.",
                        Instructions = "Aucune action réelle requise.",
                        EffectiveFromUtc = DateTimeOffset.UtcNow,
                        LastUpdatedAtUtc = DateTimeOffset.UtcNow,
                        ProvinceCode = "BE-WAL",
                        IsOfficial = false
                    };

                    await broadcaster.PublishUpsertedAsync(alert);

                    return Results.Ok(
                        new
                        {
                            success = true,
                            alert.Id
                        }
                    );
                })
            .RequireAuthorization();
        }

        app.MapPost("/_diag/emergency/official-sim",async (double latitude, double longitude, double radiusMeters, IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (latitude is < -90 or > 90)
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "Latitude must be between -90 and 90."
                    });
            }


            if (longitude is < -180 or > 180)
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "Longitude must be between -180 and 180."
                    });
            }


            /*
             * Diagnostic simulation endpoint:
             * intentionally limited to the Belgium /
             * OutZen operational bounding box.
             *
             * This also catches the common mistake:
             *
             * latitude  = 5.5667
             * longitude = 50.6333
             *
             * instead of:
             *
             * latitude  = 50.6333
             * longitude = 5.5667
             */
            const double minBelgiumLatitude = 49.45;
            const double maxBelgiumLatitude = 51.60;
            const double minBelgiumLongitude = 2.30;
            const double maxBelgiumLongitude = 6.60;


            if (
                latitude < minBelgiumLatitude
                ||
                latitude > maxBelgiumLatitude
                ||
                longitude < minBelgiumLongitude
                ||
                longitude > maxBelgiumLongitude)
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "Official simulation coordinates " +
                            "must currently be inside the " +
                            "OutZen Belgium bounding box.",

                        hint =
                            "Parameters are latitude first, " +
                            "longitude second. " +
                            "Example for Liège: " +
                            "latitude=50.6333, " +
                            "longitude=5.5667.",

                        received =
                            new
                            {
                                latitude,
                                longitude
                            }
                    });
            }

            if (radiusMeters is <= 0 or > 100_000)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "RadiusMeters must be between 0 and 100000."
                    });
            }

            var now = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString("N");
            var externalId = $"OFFICIAL-SIM-{runId}";

            /*
             * WGS84:
             *
             * X = longitude
             * Y = latitude
             */
            var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            GeoAPI.Geometries.IPoint centerPoint = geometryFactory.CreatePoint(new GeoAPI.Geometries.Coordinate(longitude, latitude));
            NetTopologySuite.Geometries.Geometry center = (NetTopologySuite.Geometries.Geometry) centerPoint;

            var payloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"OUTZEN-OFFICIAL-SIM|" + $"{externalId}|" + $"{latitude:R}|" + $"{longitude:R}|" + $"{radiusMeters:R}")));
            var alert = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                {
                    Id = Guid.NewGuid(),
                    SourceCode = "OUTZEN-OFFICIAL-SIM",
                    ExternalId = externalId,
                    ExternalReferenceId = null,
                    ReferencedExternalIds = Array.Empty<string>(),
                    CorrelationKey = $"OUTZEN-OFFICIAL-SIM:{runId}",
                    HazardType = EmergencyHazardType.Flood,
                    Severity = EmergencySeverity.Severe,
                    Urgency = EmergencyUrgency.Immediate,
                    Certainty = EmergencyCertainty.Observed,
                    Status = EmergencyAlertStatus.Active,
                    InformationKind = SafetyInformationKind.ActiveEmergency,
                    Headline = "SIMULATION — Official OutZen Alert",
                    Description = "Simulation Emergency Intelligence " + "intended to validate the chain " + "SQL, décision, SignalR and Blazor.",
                    Instructions = "SIMULATION ONLY — " + "temporarily avoid the area " + "during the test.",
                    Language = "fr-BE",
                    SentAtUtc = now,
                    EffectiveFromUtc = now,
                    ExpiresAtUtc = now.AddMinutes(20),
                    LastUpdatedAtUtc = now,

                    /*
                     * Circle representation expected by
                     * OfficialEmergencyRiskContextService.
                     */
                    Area = center,
                    RadiusMeters = radiusMeters,
                    ProvinceCode = null,
                    MunicipalityCode = null,
                    OfficialInformationUri = null,

                    /*
                     * This deliberately exercises the
                     * OFFICIAL branch of the decision engine.
                     *
                     * SourceCode clearly identifies it as SIM.
                     */
                    IsOfficial = true,
                    IsMachineVerified = false,
                    PayloadHash = payloadHash,
                    RawPayloadStorageKey = null,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

            var result = await repository.ApplyAsync(alert, cancellationToken);

            if (result.Changed && result.IsActive)
            {
                await publisher.PublishUpsertedAsync(result.StoredAlert, cancellationToken);
            }

            return Results.Ok(
                new
                {
                    success = true,
                    result.StoredAlert.Id,
                    result.StoredAlert.ExternalId,

                    Latitude = latitude,
                    Longitude = longitude,
                    RadiusMeters =radiusMeters,

                    result.StoredAlert.IsOfficial,
                    result.StoredAlert.Severity,
                    result.StoredAlert.Urgency,
                    result.StoredAlert.ExpiresAtUtc
                });
        })
        .RequireAuthorization();

        app.MapPost("/_diag/emergency/official-sim/cancel", async (string externalId,IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return Results.BadRequest(
                    new
                    {
                        error = "externalId is required."
                    });
            }

            EmergencyAlertStatus cancelledStatus;

            if (!Enum.TryParse("Cancelled", ignoreCase: true, out cancelledStatus) && !Enum.TryParse("Canceled", ignoreCase: true, out cancelledStatus))
            {
                return Results.Problem("EmergencyAlertStatus does not contain " + "Cancelled or Canceled.");
            }

            var now = DateTimeOffset.UtcNow;
            var cancelId = Guid.NewGuid().ToString("N");
            var payloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"OUTZEN-OFFICIAL-SIM-CANCEL|" + $"{externalId}|" + $"{cancelId}")));
            var cancel = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                {
                    Id = Guid.NewGuid(),
                    SourceCode = "OUTZEN-OFFICIAL-SIM",
                    ExternalId = $"OFFICIAL-SIM-CANCEL-{cancelId}",
                    ExternalReferenceId = externalId,
                    ReferencedExternalIds = new[] {externalId},
                    CorrelationKey = $"OUTZEN-OFFICIAL-SIM-CANCEL:" + $"{externalId}",
                    HazardType = EmergencyHazardType.Flood,
                    Severity = EmergencySeverity.Severe,
                    Urgency = EmergencyUrgency.Immediate,
                    Certainty = EmergencyCertainty.Observed,
                    Status = cancelledStatus,
                    InformationKind = SafetyInformationKind.ActiveEmergency,
                    Headline = "SIMULATION — Fin d'alerte",
                    Description = "Annulation de l'alerte officielle " + "simulée OutZen.",
                    Instructions = "Simulation terminée.",
                    Language = "fr-BE",
                    SentAtUtc = now,
                    EffectiveFromUtc = now,
                    ExpiresAtUtc = null,
                    LastUpdatedAtUtc = now,
                    Area = null,
                    RadiusMeters = null,
                    ProvinceCode = null,
                    MunicipalityCode = null,
                    OfficialInformationUri = null,
                    IsOfficial = true,
                    IsMachineVerified = false,
                    PayloadHash = payloadHash,
                    RawPayloadStorageKey = null,
                    IsActive = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };


            var result = await repository.ApplyAsync(cancel, cancellationToken);


            foreach (var removed in result.RemovedAlerts)
            {
                switch (removed.Reason)
                {
                    case EmergencyAlertRemovalReason.Cancelled:

                        await publisher.PublishCancelledAsync(removed.Alert, cancellationToken);

                        break;


                    case EmergencyAlertRemovalReason.Expired:

                        await publisher.PublishExpiredAsync(removed.Alert, cancellationToken);

                        break;


                    case EmergencyAlertRemovalReason.Superseded:

                        /*
                         * Not a cancellation.
                         */
                        break;
                }
            }


            return Results.Ok(
                new
                {
                    success = true,
                    CancelMessageId = result.StoredAlert.Id,
                    CancelExternalId = result.StoredAlert.ExternalId,
                    ReferencedExternalId = externalId,
                    RemovedCount = result.RemovedAlerts.Count
                });
        })
        .RequireAuthorization();

        app.MapPost("/_diag/emergency/be-alert-advisory-sim", async (IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString("N");
            var externalId = $"BE-ALERT-ADVISORY-SIM-{runId}";

            static TEnum ResolveEnum<TEnum>(params string[] candidates) where TEnum : struct, Enum
            {
                foreach (var candidate in candidates)
                {
                    if (Enum.TryParse<TEnum>(candidate, ignoreCase: true, out var value))
                    {
                        return value;
                    }
                }
                return default;
            }

            static string ComputeHash(string value)
            {
                return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
            }


            var alert = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                {
                    Id = Guid.NewGuid(),
                    SourceCode = "BE-ALERT-SIM",
                    ExternalId = externalId,
                    ExternalReferenceId = null,
                    ReferencedExternalIds = Array.Empty<string>(),
                    CorrelationKey = $"BE-ALERT-SIM:ADVISORY:{runId}",

                    /*
                     * This is public-safety information,
                     * not a critical geographic incident.
                     */
                    HazardType = default,
                    Severity = ResolveEnum<EmergencySeverity>("Moderate"),
                    Urgency = ResolveEnum<EmergencyUrgency>("Expected"),
                    Certainty = ResolveEnum<EmergencyCertainty>("Likely"),
                    Status = EmergencyAlertStatus.Active,
                    InformationKind = SafetyInformationKind.ActiveEmergency,

                    Headline = "SIMULATION BE-Alert — " + "Information au public",

                    Description = "Message de simulation destiné " + "à tester l'affichage temporaire " + "d'une consigne BE-Alert dans OutZen.",

                    Instructions =
                        "SIMULATION — En l'absence de " +
                        "danger grave ou imminent, ne " +
                        "saturez pas les lignes d'urgence. " +
                        "Pour les situations liées aux " +
                        "intempéries ne nécessitant pas " +
                        "une intervention médicale urgente, " +
                        "suivez les consignes communiquées " +
                        "par les autorités, notamment via " +
                        "le 1722 lorsqu'il est applicable.",

                    Language = "fr-BE",

                    SentAtUtc = now,
                    EffectiveFromUtc = now,
                    ExpiresAtUtc = now.AddMinutes(10),
                    LastUpdatedAtUtc = now,

                    /*
                     * Deliberately no map geometry.
                     *
                     * This message MUST NOT create
                     * an emergency map marker.
                     */
                    Area = null,
                    RadiusMeters = null,
                    ProvinceCode = null,
                    MunicipalityCode = null,
                    OfficialInformationUri = null,

                    /*
                     * true only to exercise the official
                     * branch of OutZen.
                     *
                     * SourceCode + headline clearly state
                     * that this is a simulation.
                     */
                    IsOfficial = true,
                    IsMachineVerified = false,
                    PayloadHash = ComputeHash($"BE-ALERT-ADVISORY-SIM|" + $"{externalId}"),
                    RawPayloadStorageKey = null,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };


            var result = await repository.ApplyAsync(alert, cancellationToken);

            if (result.Changed && result.IsActive)
            {
                await publisher.PublishUpsertedAsync(result.StoredAlert, cancellationToken);
            }


            return Results.Ok(
                new
                {
                    success = true,

                    result.StoredAlert.Id,
                    result.StoredAlert.ExternalId,
                    result.StoredAlert.SourceCode,
                    result.StoredAlert.Severity,
                    result.StoredAlert.Urgency,

                    HasGeometry = result.StoredAlert.Area is not null,

                    result.StoredAlert.ExpiresAtUtc
                });
        })
        .RequireAuthorization();

        app.MapPost("/_diag/emergency/be-alert-critical-sim", async (double latitude, double longitude, double radiusMeters, string hazardType, IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<EmergencyHazardType>(hazardType, ignoreCase: true, out var parsedHazardType))
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Unknown emergency hazard type " + $"'{hazardType}'.",

                        allowed = Enum.GetNames<EmergencyHazardType>()
                    });
            }
            /*
             * Basic geographic validation.
             */
            if (latitude is < -90 or > 90)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "Latitude must be between -90 and 90."
                    });
            }


            if (longitude is < -180 or > 180)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "Longitude must be between -180 and 180."
                    });
            }

            if (radiusMeters <= 0 || radiusMeters > 100_000)
            {
                return Results.BadRequest(
                    new
                    {
                        error = "RadiusMeters must be between " + "0 and 100000."
                    });
            }


            var now = DateTimeOffset.UtcNow;

            var runId = Guid.NewGuid().ToString("N");

            var externalId = $"BE-ALERT-CRITICAL-SIM-{runId}";


            static string ComputeHash(string value)
            {
                return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
            }

            /*
             * Keep compatibility with the current
             * NTS / GeoAPI package mix.
             */
            var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            GeoAPI.Geometries.IPoint centerPoint = geometryFactory.CreatePoint(new GeoAPI.Geometries.Coordinate(longitude, latitude));
            NetTopologySuite.Geometries.Geometry center = (NetTopologySuite.Geometries.Geometry) centerPoint;


            var alert = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                {
                    Id = Guid.NewGuid(),
                    SourceCode = "BE-ALERT-SIM",
                    ExternalId = externalId,
                    ExternalReferenceId = null,
                    ReferencedExternalIds = Array.Empty<string>(),
                    CorrelationKey = $"BE-ALERT-SIM:CRITICAL:{runId}",
                    HazardType = parsedHazardType,
                    Severity = EmergencySeverity.Severe,
                    Urgency = EmergencyUrgency.Immediate,
                    Certainty = EmergencyCertainty.Observed,
                    Status = EmergencyAlertStatus.Active,
                    InformationKind = SafetyInformationKind.ActiveEmergency,

                    Headline = "SIMULATION BE-Alert — " + "Situation critique",

                    Description =
                        "SIMULATION — Une situation désastreuse " +
                        "majeure est en cours dans la zone. " +
                        "Cette alerte teste simultanément " +
                        "le message public et le marqueur " +
                        "Emergency OutZen.",

                    Instructions =
                        "SIMULATION — Évitez la zone, " +
                        "respectez les périmètres établis " +
                        "par les services de secours et " +
                        "suivez uniquement les consignes " +
                        "officielles.",

                    Language = "fr-BE",

                    SentAtUtc = now,
                    EffectiveFromUtc = now,
                    ExpiresAtUtc = now.AddMinutes(20),
                    LastUpdatedAtUtc = now,

                    Area = center,
                    RadiusMeters = radiusMeters,
                    ProvinceCode = null,
                    MunicipalityCode = null,
                    OfficialInformationUri = null,

                    IsOfficial = true,
                    IsMachineVerified = false,

                    PayloadHash = ComputeHash($"BE-ALERT-CRITICAL-SIM|" + $"{externalId}|" + $"{parsedHazardType}|" + $"{latitude:R}|" + $"{longitude:R}|" + $"{radiusMeters:R}"),

                    RawPayloadStorageKey = null,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };


            var result = await repository.ApplyAsync(alert, cancellationToken);

            if (result.Changed && result.IsActive)
            {
                await publisher.PublishUpsertedAsync(result.StoredAlert, cancellationToken);
            }


            return Results.Ok(
                new
                {
                    success = true,

                    result.StoredAlert.Id,
                    result.StoredAlert.ExternalId,
                    result.StoredAlert.SourceCode,

                    HazardType = result.StoredAlert.HazardType.ToString(),
                    HazardValue = (int)result.StoredAlert.HazardType,

                    latitude,
                    longitude,
                    radiusMeters,

                    result.StoredAlert.Severity,
                    result.StoredAlert.Urgency,
                    result.StoredAlert.ExpiresAtUtc
                });
        })
        .RequireAuthorization();

        app.MapPost("/_diag/emergency/be-alert-cancel-sim", async (string externalId, IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return Results.BadRequest(
                    new
                    {
                        error = "externalId is required."
                    });
            }

            var now = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid().ToString("N");
            var cancelExternalId = $"BE-ALERT-CANCEL-SIM-{runId}";

            static string ComputeHash(string value)
            {
                return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
            }


            var cancelAlert = new CitizenHackathon2025.EmergencyIntelligence.Models.EmergencyAlert
                {
                    Id = Guid.NewGuid(),

                    /*
                     * CRITICAL:
                     *
                     * Same source as the alert
                     * being cancelled.
                     */
                    SourceCode = "BE-ALERT-SIM",
                    ExternalId = cancelExternalId,
                    ExternalReferenceId = externalId,
                    ReferencedExternalIds = new[] {externalId},
                    CorrelationKey = $"BE-ALERT-SIM:CANCEL:{externalId}",
                    HazardType = default,
                    Severity = default,
                    Urgency = default,
                    Certainty = default,
                    Status = EmergencyAlertStatus.Cancelled,
                    InformationKind = SafetyInformationKind.ActiveEmergency,

                    Headline = "SIMULATION BE-Alert — " + "Fin d'alerte",
                    Description = "Message de simulation " + "annulant une alerte BE-Alert.",
                    Instructions = null,
                    Language = "fr-BE",

                    SentAtUtc = now,
                    EffectiveFromUtc = now,
                    ExpiresAtUtc = now,
                    LastUpdatedAtUtc = now,
                    Area = null,
                    RadiusMeters = null,
                    ProvinceCode = null,
                    MunicipalityCode = null,
                    OfficialInformationUri = null,
                    IsOfficial = true,
                    IsMachineVerified = false,
                    PayloadHash = ComputeHash($"BE-ALERT-CANCEL-SIM|" + $"{cancelExternalId}|" + $"{externalId}"),
                    RawPayloadStorageKey = null,
                    IsActive = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };


            var result = await repository.ApplyAsync(cancelAlert, cancellationToken);

            /*
             * Publish cancellation of every alert
             * actually removed by the lifecycle.
             */
            foreach (var removal in result.RemovedAlerts)
            {
                if (
                    removal.Reason != EmergencyAlertRemovalReason.Cancelled)
                {
                    continue;
                }

                await publisher.PublishCancelledAsync(removal.Alert, cancellationToken);
            }

            return Results.Ok(
                new
                {
                    success = true,

                    cancelMessageId = result.StoredAlert.Id,

                    cancelExternalId,

                    referencedExternalId = externalId,

                    removedCount = result.RemovedAlerts.Count,

                    removed = result.RemovedAlerts
                        .Select(
                            x => new
                            {
                                x.Alert.Id,
                                x.Alert.SourceCode,
                                x.Alert.ExternalId,

                                reason = x.Reason.ToString()
                            })
                        .ToArray()
                });
        })
        .RequireAuthorization();

        app.MapGet("/_diag/emergency/hazard-types", () =>
        {
            return Results.Ok(Enum.GetNames<EmergencyHazardType>());
        })
        .RequireAuthorization();

        app.MapGet("/", () => "OK");
    }
}































































































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.