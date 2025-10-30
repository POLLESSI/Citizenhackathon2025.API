CREATE PROCEDURE [dbo].[GetAntennaCounts]
    @AntennaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(*) AS ActiveConnections,
        COUNT(DISTINCT DeviceHash) AS UniqueDevices -- DeviceHash should approximate unique users
    FROM dbo.CrowdInfoAntennaConnection
    WHERE AntennaId = @AntennaId
      AND Active = 1;
END;

GO