CREATE TABLE [dbo].[CrowdInfoAntennaConnection]
(
    Id BIGINT IDENTITY PRIMARY KEY,
    AntennaId INT NOT NULL REFERENCES dbo.CrowdInfoAntenna(Id),
    DeviceHash BINARY(32) NOT NULL,         -- HMAC-SHA256
    IpHash BINARY(32) NULL,
    MacHash BINARY(32) NULL,
    Source TINYINT NOT NULL DEFAULT 0,
    SignalStrength SMALLINT NULL,
    Band NVARCHAR(16) NULL,
    FirstSeenUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LastSeenUtc DATETIME2(3) NOT NULL,
    Active BIT DEFAULT 1,
    AdditionalJson NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_CrowdInfoAntennaConnection_Device_Antenna UNIQUE (AntennaId, DeviceHash)
);

GO

CREATE INDEX IX_CrowdInfoAntennaConnection_Antenna_LastSeen
ON dbo.CrowdInfoAntennaConnection (AntennaId, LastSeenUtc DESC)
WHERE Active = 1;

GO

CREATE TRIGGER dbo.OnDeleteCrowdInfoAntennaConnection
ON dbo.CrowdInfoAntennaConnection
INSTEAD OF DELETE
AS
BEGIN
    UPDATE dbo.CrowdInfoAntennaConnection
    SET Active = 0
    WHERE Id IN (SELECT Id FROM deleted);
END;
GO