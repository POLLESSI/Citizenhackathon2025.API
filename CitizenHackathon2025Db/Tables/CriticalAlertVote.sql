CREATE TABLE [dbo].[CriticalAlertVote]
(
	[Id] BIGINT IDENTITY(1,1) NOT NULL,
	[AlertKind] TINYINT NOT NULL, -- 1 Crowd, 2 Weather, 3 Traffic
    [ZoneKey] NVARCHAR(64) NOT NULL,

    [PlaceId] INT NULL,
    [UserId] INT NULL,
    [DeviceHash] NVARCHAR(128) NULL,
    [IpHash] NVARCHAR(128) NULL,

    [Latitude] DECIMAL(9,6) NOT NULL,
    [Longitude] DECIMAL(9,6) NOT NULL,
    [Reason] NVARCHAR(256) NULL,

    [CreatedAtUtc] DATETIME2(3) NOT NULL
        CONSTRAINT DF_CriticalAlertVote_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    [Active] BIT NOT NULL
        CONSTRAINT DF_CriticalAlertVote_Active DEFAULT 1,

    CONSTRAINT PK_CriticalAlertVote PRIMARY KEY (Id)
);
GO

CREATE INDEX IX_CriticalAlertVote_Kind_Zone_CreatedAt
ON dbo.CriticalAlertVote(AlertKind, ZoneKey, CreatedAtUtc DESC)
INCLUDE (UserId, DeviceHash, IpHash, Active);
GO