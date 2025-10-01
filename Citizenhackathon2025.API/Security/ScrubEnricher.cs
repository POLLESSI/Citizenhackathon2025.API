using Serilog.Core;
using Serilog.Events;

namespace CitizenHackathon2025.API.Security
{
    public class ScrubEnricher : ILogEventEnricher
    {
        private readonly string _propertyName;
        private readonly string _value;

        public ScrubEnricher(string propertyName, string value)
        {
            _propertyName = propertyName;
            _value = LogMasking.Scrub(value);
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_propertyName, _value));
        }
    }
}
