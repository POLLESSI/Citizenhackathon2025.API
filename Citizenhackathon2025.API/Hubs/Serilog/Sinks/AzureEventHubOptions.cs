using System;

namespace CitizenHackathon2025.API.Hubs.Serilog.Sinks
{
    public sealed class AzureEventHubOptions
    {
        public string? ConnectionString { get; set; }
        public string? EventHubName { get; set; }
        public int BatchSizeLimit { get; set; } = 100;
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(2);

        // Important: Use global:: to avoid masking by your "Serilog" namespace
        public Func<global::Serilog.Events.LogEvent, string?>? PartitionKeyResolver { get; set; }
    }
}
