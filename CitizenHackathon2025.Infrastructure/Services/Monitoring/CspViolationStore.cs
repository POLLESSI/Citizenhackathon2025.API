using CitizenHackathon2025.DTOs.Security;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CitizenHackathon2025.Infrastructure.Services.Monitoring
{
    public class CspViolationStore
    {
        private readonly List<CspReportContent> _reports = new();
        private readonly object _lock = new();

        public void Add(CspReportContent report)
        {
            lock (_lock)
            {
                if (_reports.Count >= 1000)
                    _reports.RemoveAt(0); 
                _reports.Add(report);
            }
        }

        public List<CspReportContent> GetAll()
        {
            lock (_lock)
            {
                return _reports.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _reports.Clear();
            }
        }
    }
}

































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.