using CitizenHackathon2025.Application.Options;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdCriticalAlertService
    {
        private readonly CriticalAlertRules _rules;

        public CrowdCriticalAlertService(IOptions<CriticalAlertRules> options)
        {
            _rules = options.Value;
        }
    }
}


















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.