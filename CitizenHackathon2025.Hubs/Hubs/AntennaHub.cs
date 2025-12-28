using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Shared.Interfaces;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Polly;
using System.Data;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class AntennaHub : Hub
    {
    #nullable disable
        private readonly IDeviceHasher _hasher;
        private readonly IDbConnection _db;

        public AntennaHub(IDeviceHasher hasher, IDbConnection db)
        {
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task ReportHeartbeat(int antennaId, string deviceId = null, byte[] deviceHash = null,
            byte[] ipHash = null, byte[] macHash = null, int? signal = null, string band = null)
        {
            // 1) Resolve deviceHash: if deviceId is provided, hash it on the server side.
            if (deviceHash == null)
            {
                if (string.IsNullOrEmpty(deviceId))
                    throw new ArgumentException("deviceId or deviceHash must be provided");

                deviceHash = _hasher.ComputeHash(deviceId);
            }

            // 2) Prepare Dapper parameters
            var p = new DynamicParameters();
            p.Add("@AntennaId", antennaId, DbType.Int32);
            p.Add("@DeviceHash", deviceHash, DbType.Binary, size: 32);
            p.Add("@IpHash", ipHash, DbType.Binary, size: 32);
            p.Add("@MacHash", macHash, DbType.Binary, size: 32);
            p.Add("@SignalStrength", signal, DbType.Int16);
            p.Add("@Band", band, DbType.String);

            // 3) Call proc via Dapper on the connection
            //    Make sure to open the connection if necessary (according to DI).
            if (_db.State != ConnectionState.Open) _db.Open();

            await _db.ExecuteAsync("dbo.UpsertAntennaConnection", p, commandType: CommandType.StoredProcedure);

            var counts = await _db.QueryFirstAsync<AntennaCountsDTO>(
                "dbo.GetAntennaCounts",
                new { AntennaId = antennaId },
                commandType: CommandType.StoredProcedure);

            // 4) SignalR groups / broadcast
            await Groups.AddToGroupAsync(Context.ConnectionId, $"antenna_{antennaId}");
            await Clients.Group($"antenna_{antennaId}").SendAsync("AntennaCountsUpdated", antennaId, counts);
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.