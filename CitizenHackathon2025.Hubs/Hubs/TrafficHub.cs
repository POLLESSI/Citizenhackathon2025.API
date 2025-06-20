﻿using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public class TrafficHub : Hub
    {
#nullable disable

        private readonly ILogger<TrafficHub> _logger;

        public TrafficHub(ILogger<TrafficHub> logger)
        {
            _logger = logger;
        }

        public async Task RefreshTraffic()
        {
            _logger.LogInformation("RefreshTraffic called");
            await Clients.All.SendAsync("notifynewtraffic");
        }
    }
}











































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.