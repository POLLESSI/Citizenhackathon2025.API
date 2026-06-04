CREATE TABLE [dbo].[CrowdInfo]
(
	[Id] INT IDENTITY,
	[LocationName] NVARCHAR(64) NOT NULL,
	[Latitude] DECIMAL(9, 6) NOT NULL,
	[Longitude] DECIMAL(9, 6) NOT NULL,
	[CrowdLevel] INT NOT NULL,
	[Timestamp] DATETIME2(0) NOT NULL,
	[Active] BIT DEFAULT 1,

	[IsManualCriticalAlert] BIT NOT NULL 
        CONSTRAINT [DF_CrowdInfo_IsManualCriticalAlert] DEFAULT 0,

    [ExpiresAtUtc] DATETIME2(3) NULL,
    [Source] NVARCHAR(32) NULL,
    [Reason] NVARCHAR(256) NULL,

	CONSTRAINT [PK_CrowdInfo] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_CrowdInfo_LocationName_Timestamp] UNIQUE (LocationName, [Timestamp])
)

GO

CREATE TRIGGER [dbo].[OnDeleteCrowdInfo]
	ON [dbo].[CrowdInfo]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE CrowdInfo SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END

GO

CREATE INDEX IX_CrowdInfo_Active_Timestamp_Include
ON dbo.CrowdInfo(Active, [Timestamp] DESC)
INCLUDE (
    LocationName,
    Latitude,
    Longitude,
    CrowdLevel,
    IsManualCriticalAlert,
    ExpiresAtUtc,
    Source,
    Reason
);

GO

CREATE INDEX IX_CrowdInfo_Active_Pos
ON dbo.CrowdInfo(Active, LocationName, Latitude, Longitude);

GO



















































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.