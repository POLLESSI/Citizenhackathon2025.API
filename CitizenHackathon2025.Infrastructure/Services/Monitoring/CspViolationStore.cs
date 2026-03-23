using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Security;

namespace CitizenHackathon2025.Infrastructure.Services.Monitoring
{
    public sealed class CspViolationStore : ICspViolationStore
    {
        private readonly List<CspReportContent> _items = new();
        private readonly object _lock = new();

        public void Add(CspReportContent report)
        {
            if (report is null) return;

            lock (_lock)
            {
                _items.Add(report);
            }
        }

        public IEnumerable<CspReportContent> GetAll()
        {
            lock (_lock)
            {
                return _items.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
            }
        }
    }
}

































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.