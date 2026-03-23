CREATE TABLE [dbo].[Event]
(
	[Id] INT IDENTITY,
    [Name] NVARCHAR(64) NOT NULL,
	[PlaceId] INT NULL,
	[Latitude] DECIMAL(9, 6) NOT NULL,
	[Longitude] DECIMAL(9, 6) NOT NULL,
	[DateEvent] DATETIME2(0) NOT NULL,
	[ExpectedCrowd] INT NULL,
	[IsOutdoor] BIT NULL,
	[ExternalSource] NVARCHAR(32) NULL,
	[ExternalId] NVARCHAR(128) NULL,
	[SourceUpdatedAtUtc] DATETIME2(3) NULL,
	[Active] BIT DEFAULT 1,

	CONSTRAINT [PK_Event] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Event_Place] FOREIGN KEY ([PlaceId]) REFERENCES [dbo].[Place]([Id]) ON DELETE SET NULL,
	CONSTRAINT [UQ_Event_Name_DateEvent] UNIQUE ([Name], [DateEvent]),
	CONSTRAINT CK_Event_Lat CHECK (Latitude  BETWEEN -90  AND 90),
	CONSTRAINT CK_Event_Lon CHECK (Longitude BETWEEN -180 AND 180)

)

GO

CREATE TRIGGER [dbo].[OnDeleteEvent]
	ON [dbo].[Event]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE Event SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END

GO

CREATE INDEX IX_Event_Active_DateEvent
ON dbo.Event (Active, DateEvent DESC);
GO

CREATE UNIQUE INDEX UX_Event_ExternalSource_ExternalId
ON dbo.Event(ExternalSource, ExternalId)
WHERE ExternalSource IS NOT NULL AND ExternalId IS NOT NULL;
GO









































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.