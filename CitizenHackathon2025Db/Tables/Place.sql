CREATE TABLE [dbo].[Place]
(
	[Id] INT IDENTITY,
	[Name] NVARCHAR (64),
	[Type] NVARCHAR (32),
	[Indoor] BIT,
	[Latitude] DECIMAL(9, 6),
	[Longitude] DECIMAL(9, 6),
	[Capacity] INT,
	[Tag] NVARCHAR(16),
	[Active] BIT DEFAULT 1

	CONSTRAINT [PK_Place] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_Place_Name] UNIQUE ([Name])
)

GO

CREATE TRIGGER [dbo].[OnDeletePlace]
	ON [dbo].[Place]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE Place SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END

GO

CREATE INDEX IX_Place_Active_Id ON dbo.Place (Active, Id DESC);
GO























































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.