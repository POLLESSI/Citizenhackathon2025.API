using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class WeatherAlertRepository : IWeatherAlertRepository
    {
        private readonly IDbConnection _cn;

        public WeatherAlertRepository(IDbConnection cn) => _cn = cn;

        public Task<WeatherAlertEntity?> UpsertAsync(WeatherAlertEntity a, CancellationToken ct = default)
        {
            const string sql = @"EXEC dbo.sp_WeatherAlert_Upsert
                @Provider, @ExternalId, @Latitude, @Longitude, @SenderName, @EventName,
                @StartUtc, @EndUtc, @Description, @Tags, @Severity, @LastSeenAt;";

            return _cn.QueryFirstOrDefaultAsync<WeatherAlertEntity>(
                new CommandDefinition(sql, a, cancellationToken: ct));
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.