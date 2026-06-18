CREATE TABLE dbo.CrowdAlertVote
(
    [Id] INT IDENTITY PRIMARY KEY,
    [PlaceId] INT NOT NULL,
    [ZoneKey] NVARCHAR(64) NOT NULL,

    [UserId] INT NULL,
    [DeviceHash] NVARCHAR(128) NULL,
    [IpHash] NVARCHAR(128) NULL,
    [Latitude] DECIMAL(9,6) NOT NULL,
    [Longitude] DECIMAL(9,6) NOT NULL,

    [Reason] NVARCHAR(256) NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [Active] BIT NOT NULL DEFAULT 1
);
GO

CREATE INDEX IX_CrowdAlertVote_Zone_Time
ON dbo.CrowdAlertVote([ZoneKey], [CreatedAtUtc] DESC)
INCLUDE ([PlaceId], [UserId], [DeviceHash], [IpHash]);
GO