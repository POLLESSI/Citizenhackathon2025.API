CREATE TABLE [dbo].[Event]
(
	[Id] INT IDENTITY,
    [Name] NVARCHAR(64),
	[PlaceId] INT NULL,
	[Latitude] DECIMAL(9, 2),
	[Longitude] DECIMAL(9, 3),
	[DateEvent] DATETIME2(0),
	[ExpectedCrowd] INT,
	[IsOutdoor] BIT,
	[Active] BIT DEFAULT 1

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











































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.