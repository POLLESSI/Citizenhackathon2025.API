using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;

namespace CitizenHackathon2025.API.Hubs.Serilog.Sinks
{
    public static class LoggerConfigurationAzureEventHubExtensions
    {
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration sinkConfiguration,
            AzureEventHubOptions options,
            ITextFormatter? formatter = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
        {
            return sinkConfiguration.Sink(
                new AzureEventHubSink(options, formatter),
                restrictedToMinimumLevel);
        }
    }
}
