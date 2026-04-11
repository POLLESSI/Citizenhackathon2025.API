CREATE VIEW [dbo].[vw_AntennaCounts]
AS
SELECT AntennaId,
       COUNT(*) AS ActiveConnections,
       COUNT(DISTINCT DeviceHash) AS UniqueDevices,
       MAX(LastSeenUtc) AS LastSeenUtc
FROM dbo.CrowdInfoAntennaConnection
WHERE Active = 1
GROUP BY AntennaId;
GO



































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.