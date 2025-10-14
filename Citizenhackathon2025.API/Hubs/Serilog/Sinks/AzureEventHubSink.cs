using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
// ⚠️ DO NOT reference Microsoft.AspNetCore.Mvc.Diagnostics
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.PeriodicBatching;

namespace CitizenHackathon2025.API.Hubs.Serilog.Sinks
{
    /// <summary>
    /// Serilog sink → Azure Event Hubs in batches (SDK EH v5, .NET 6/8).
    /// </summary>
    public sealed class AzureEventHubSink : PeriodicBatchingSink
    {
        private readonly EventHubProducerClient _producer;
        private readonly ITextFormatter _formatter;
        private readonly Func<LogEvent, string?>? _partitionKeyResolver;

        public AzureEventHubSink(AzureEventHubOptions options, ITextFormatter? formatter = null)
            // NOTE: If you are using PeriodicBatching v3.x, use the ctor with PeriodicBatchingSinkOptions*
            : base(Math.Max(1, options.BatchSizeLimit), options.Period)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new ArgumentException("AzureEventHub: Missing ConnectionString.", nameof(options.ConnectionString));

            // Basic local validation (without ctx)
            var parsed = EventHubsConnectionStringProperties.Parse(options.ConnectionString);
            if (parsed.Endpoint == null || string.IsNullOrWhiteSpace(parsed.Endpoint.Host))
                throw new ArgumentException("AzureEventHub: Endpoint host missing in ConnectionString.");
            if (parsed.Endpoint.Host.Contains('<') || parsed.Endpoint.Host.Contains('>'))
                throw new ArgumentException("AzureEventHub: ConnectionString contains placeholders (<...>). Replace them.");
            if (string.IsNullOrWhiteSpace(parsed.EventHubName) && string.IsNullOrWhiteSpace(options.EventHubName))
                throw new ArgumentException("AzureEventHub: specify EntityPath in ConnectionString OR set EventHubName.");

            _producer = string.IsNullOrWhiteSpace(options.EventHubName)
                ? new EventHubProducerClient(options.ConnectionString)
                : new EventHubProducerClient(options.ConnectionString, options.EventHubName);

            _formatter = formatter ?? new CompactJsonFormatter();
            _partitionKeyResolver = options.PartitionKeyResolver;
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            EventDataBatch? batch = null;
            string? currentPk = null;

            try
            {
                foreach (var logEvent in events)
                {
                    var pk = _partitionKeyResolver?.Invoke(logEvent);

                    if (batch is null || currentPk != pk)
                    {
                        await FlushAsync(batch).ConfigureAwait(false);
                        batch = await CreateBatchAsync(pk).ConfigureAwait(false);
                        currentPk = pk;
                    }

                    var ed = new EventData(Format(logEvent));

                    if (!batch.TryAdd(ed))
                    {
                        await _producer.SendAsync(batch).ConfigureAwait(false);
                        batch.Dispose();
                        batch = await CreateBatchAsync(currentPk).ConfigureAwait(false);

                        if (!batch.TryAdd(ed))
                        {
                            // Event > batch capacity → isolated sending
                            await _producer.SendAsync(new[] { ed }).ConfigureAwait(false);
                        }
                    }
                }

                await FlushAsync(batch).ConfigureAwait(false);
            }
            finally
            {
                batch?.Dispose();
            }
        }

        private ValueTask<EventDataBatch> CreateBatchAsync(string? partitionKey) =>
            partitionKey is null
                ? _producer.CreateBatchAsync()
                : _producer.CreateBatchAsync(new CreateBatchOptions { PartitionKey = partitionKey });

        private async Task FlushAsync(EventDataBatch? batch)
        {
            if (batch is not null && batch.Count > 0)
            {
                await _producer.SendAsync(batch).ConfigureAwait(false);
                batch.Dispose();
            }
        }

        private ReadOnlyMemory<byte> Format(LogEvent logEvent)
        {
            using var sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            var json = sw.ToString();
            return new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(json));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _producer.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}