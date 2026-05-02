CREATE TABLE [dbo].[CrowdInfoAntenna]
(
	[Id] INT IDENTITY,
	[Name] NVARCHAR(64) NULL,
    [Latitude] DECIMAL(9,6) NOT NULL,
    [Longitude] DECIMAL(9,6) NOT NULL,
    [GeoLocation] AS geography::Point(CONVERT(float, Latitude), CONVERT(float, Longitude), 4326) PERSISTED,
    [Active] BIT DEFAULT(1),
    [CreatedUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [Description] NVARCHAR(256) NULL,
	[MaxCapacity] INT NULL,
	[ExternalSource] NVARCHAR(32) NULL,
	[ExternalId] NVARCHAR(128) NULL,
	[LastSyncedUtc] DATETIME2(3) NULL,

	CONSTRAINT [PK_CrowdInfoAntenna] PRIMARY KEY ([Id])
)

GO

CREATE SPATIAL INDEX IX_CrowdInfoAntenna_Geo ON dbo.CrowdInfoAntenna(GeoLocation)
WITH (GRIDS =(LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW), CELLS_PER_OBJECT = 16);

GO

CREATE UNIQUE INDEX UX_CrowdInfoAntenna_Source_ExternalId
ON dbo.CrowdInfoAntenna(ExternalSource, ExternalId)
WHERE ExternalSource IS NOT NULL
  AND ExternalId IS NOT NULL;
GO

CREATE TRIGGER [dbo].[OnDeleteCrowdInfoAntenna]
	ON [dbo].[CrowdInfoAntenna]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE CrowdInfoAntenna SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END
GO










































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.