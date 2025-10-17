using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CitizenHackathon2025.API.Hubs.Serilog.Sinks
{
    /// <summary>
    /// Facade to configure Serilog with Azure Event Hubs sink (+ self-test).
    /// Typical usage (.NET 8):
    ///   AzureEventHub.ConfigureSerilog(builder.Configuration);
    /// </summary>
    public static class AzureEventHub
    {
        private const string DefaultSection = "Logging:AzureEventHub";

        /// <summary>
        /// Configure Serilog (Log.Logger) with Azure Event Hubs from configuration.
        /// - Default section: "Logging:AzureEventHub"
        /// - Use CompactJsonFormatter if not specified
        /// </summary>
        public static void ConfigureSerilog(IConfiguration configuration, string sectionName = DefaultSection, LogEventLevel minimumLevel = LogEventLevel.Information)
        {
            // ✅ get the section here
            var section = configuration.GetSection(sectionName);

            var options = BindAndValidateOptions(configuration, sectionName);

            // ✅ override PeriodSeconds BEFORE creating the logger
            var periodSecondsStr = section["PeriodSeconds"];
            if (!string.IsNullOrWhiteSpace(periodSecondsStr) &&
                int.TryParse(periodSecondsStr, out var sec) && sec > 0)
            {
                options.Period = TimeSpan.FromSeconds(sec);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .Enrich.FromLogContext()
                .WriteTo.AzureEventHub(
                    options,
                    formatter: new CompactJsonFormatter(),
                    restrictedToMinimumLevel: minimumLevel)
                .CreateLogger();
        }

        /// <summary>
        /// Variant to plug into an existing LoggerConfiguration (eg: UseSerilog((ctx, lc) => ...)).
        /// </summary>
        public static LoggerConfiguration WriteToAzureEventHub(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            string sectionName = DefaultSection,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
        {
            var options = BindAndValidateOptions(configuration, sectionName);
            return loggerConfiguration
                .WriteTo.AzureEventHub(options, new CompactJsonFormatter(), restrictedToMinimumLevel);
        }

        /// <summary>
        /// Registers an IHostedService that performs an Event Hubs connectivity self-test on startup.
        /// Useful in pre-production/production to detect bad secrets or rights.
        /// </summary>
        public static IServiceCollection AddEventHubSelfTest(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = DefaultSection)
        {
            var options = BindAndValidateOptions(configuration, sectionName);
            services.AddSingleton(options);
            services.AddHostedService<EventHubSelfTestHostedService>();
            return services;
        }

        /// <summary>
        /// Sends a "ping" message to validate connectivity and partitioning.
        /// Can be called manually (diagnostics).
        /// </summary>
        public static async Task SendPingAsync(AzureEventHubOptions options, CancellationToken ct = default)
        {
            var producer = string.IsNullOrWhiteSpace(options.EventHubName)
                ? new EventHubProducerClient(options.ConnectionString)
                : new EventHubProducerClient(options.ConnectionString, options.EventHubName);

            try
            {
                using var batch = await producer.CreateBatchAsync(cancellationToken: ct).ConfigureAwait(false);
                var payload = System.Text.Encoding.UTF8.GetBytes($"\"ping\":\"{DateTimeOffset.UtcNow:O}\"");
                var ed = new EventData(payload);
                if (!batch.TryAdd(ed))
                    throw new InvalidOperationException("Ping event does not fit batch capacity.");

                await producer.SendAsync(batch, ct).ConfigureAwait(false);
            }
            finally
            {
                await producer.DisposeAsync().AsTask().ConfigureAwait(false);
            }
        }

        private static AzureEventHubOptions BindAndValidateOptions(IConfiguration configuration, string sectionName)
        {
            var section = configuration.GetSection(sectionName);
            var options = new AzureEventHubOptions();
            section.Bind(options);

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new ArgumentException($"[{sectionName}] ConnectionString is required.");

            // (Optional) Simple Normalization
            options.ConnectionString = options.ConnectionString.Trim();

            // ✅ accepts PeriodSeconds in addition to Period
            var periodSecondsStr = section["PeriodSeconds"];
            if (!string.IsNullOrWhiteSpace(periodSecondsStr) &&
                int.TryParse(periodSecondsStr, out var sec) && sec > 0)
            {
                options.Period = TimeSpan.FromSeconds(sec);
            }

            // Event Hubs Validation
            var parsed = EventHubsConnectionStringProperties.Parse(options.ConnectionString);
            if (parsed.Endpoint == null || string.IsNullOrWhiteSpace(parsed.Endpoint.Host))
                throw new ArgumentException($"[{sectionName}] Endpoint host missing in ConnectionString.");
            if (parsed.Endpoint.Host.Contains('<') || parsed.Endpoint.Host.Contains('>'))
                throw new ArgumentException($"[{sectionName}] ConnectionString contains placeholders (<...>).");

            if (string.IsNullOrWhiteSpace(parsed.EventHubName) &&
                string.IsNullOrWhiteSpace(options.EventHubName))
                throw new ArgumentException($"[{sectionName}] Specify EntityPath in ConnectionString OR set EventHubName.");

            if (options.BatchSizeLimit <= 0)
                options.BatchSizeLimit = 100;

            if (options.Period <= TimeSpan.Zero)
                options.Period = TimeSpan.FromSeconds(2);

            return options;
        }

        /// <summary>
        /// Minimal hosted service that attempts a ping upon startup.
        /// </summary>
        private sealed class EventHubSelfTestHostedService : IHostedService
        {
            private readonly AzureEventHubOptions _options;

            public EventHubSelfTestHostedService(AzureEventHubOptions options)
            {
                _options = options;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                try
                {
                    await SendPingAsync(_options, cancellationToken).ConfigureAwait(false);
                    Log.Information("Azure Event Hubs self-test succeeded.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Azure Event Hubs self-test failed.");
                    // It's up to you: throw to stop the process, or just log.
                    // throw;
                }
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}











































































