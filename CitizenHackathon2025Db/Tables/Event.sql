CREATE TABLE [dbo].[Event]
(
	[Id] INT IDENTITY,
    [Name] NVARCHAR(64),
	[Latitude] DECIMAL(8, 2),
	[Longitude] DECIMAL(9, 3),
	[DateEvent] DATE,
	[ExpectedCrowd] INT,
	[IsOutdoor] BIT,
	[Active] BIT DEFAULT 1

	CONSTRAINT [PK_Event] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_Event_Name_DateEvent] UNIQUE ([Name], [DateEvent])
)

GO

CREATE TRIGGER [dbo].[OnDeleteEvent]
	ON [dbo].[Event]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE Event SET Active = 0
		WHERE Id = (SELECT Id FROM deleted)
	END












































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.