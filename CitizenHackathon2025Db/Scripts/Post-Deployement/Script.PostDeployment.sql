-- ==========================================
-- Post-Deployment (DEV seed)
-- ==========================================

-- Seed antennes DEV (si aucune)
IF NOT EXISTS (SELECT 1 FROM dbo.CrowdInfoAntenna)
BEGIN
    INSERT dbo.CrowdInfoAntenna([Name],[Latitude],[Longitude],[Description],[MaxCapacity])
    VALUES
      (N'DEV Antenna 1', 50.467000, 4.867000, N'Antenne DEV centre', NULL),
      (N'DEV Antenna 2', 50.466200, 4.872500, N'Antenne DEV nord',   NULL),
      (N'DEV Antenna 3', 50.463800, 4.860900, N'Antenne DEV sud',    NULL);
END

-- Seed snapshots “frais” (juste pour tester Leaflet)
DECLARE @now DATETIME2(3) = SYSUTCDATETIME();
DECLARE @ws  DATETIME2(3) = DATEADD(SECOND, - (DATEPART(SECOND, @now) % 10), @now); -- arrondi 10s

IF NOT EXISTS (SELECT 1 FROM dbo.CrowdInfoAntennaSnapshot WHERE WindowStartUtc >= DATEADD(MINUTE,-2,@now))
BEGIN
    INSERT dbo.CrowdInfoAntennaSnapshot
        (AntennaId, WindowStartUtc, WindowSeconds, ActiveConnections, Confidence, Source, CreatedUtc)
    SELECT
        A.Id, @ws, 10,
        CASE A.[Name]
            WHEN N'DEV Antenna 1' THEN 12
            WHEN N'DEV Antenna 2' THEN 58
            ELSE 5
        END,
        80, 1, SYSUTCDATETIME()
    FROM dbo.CrowdInfoAntenna A;
END
GO









































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.