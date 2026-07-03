//namespace CitizenHackathon2025.Application.Intelligence.AlertFusion
//{
//    public sealed class CrowdAlertCluster
//    {
//        public Guid Id { get; set; } = Guid.NewGuid();

//        public string ZoneName { get; set; } = "Unknown zone";

//        public double Latitude { get; set; }
//        public double Longitude { get; set; }

//        /// <summary>
//        /// Overall severity (0-4)
//        /// </summary>
//        public byte Severity { get; set; }

//        /// <summary>
//        /// Sum of all active connections inside the cluster.
//        /// </summary>
//        public int TotalActiveConnections { get; set; }

//        /// <summary>
//        /// Sum of all unique devices.
//        /// </summary>
//        public int TotalUniqueDevices { get; set; }

//        /// <summary>
//        /// Number of antennas involved.
//        /// </summary>
//        public int AntennaCount { get; set; }

//        public List<int> AntennaIds { get; set; } = new();

//        public List<long> AlertIds { get; set; } = new();

//        public DateTime FirstDetectedAtUtc { get; set; }

//        public DateTime LastDetectedAtUtc { get; set; }

//        public string Status { get; set; } = "PendingValidation";

//        /// <summary>
//        /// Final score computed by RiskScoreCalculator.
//        /// </summary>
//        public int RiskScore { get; set; }

//        /// <summary>
//        /// Human readable summary.
//        /// </summary>
//        public string Message { get; set; } = string.Empty;
//    }
//}

























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.