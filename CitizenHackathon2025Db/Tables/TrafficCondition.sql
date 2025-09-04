CREATE TABLE [dbo].[TrafficCondition]
(
	[Id] INT IDENTITY,
	[Latitude] DECIMAL(8, 2),
	[Longitude] DECIMAL(9, 3),
	[DateCondition] DATE,
	[CongestionLevel] NVARCHAR(2),
	[IncidentType] NVARCHAR(64),
	[Active] BIT DEFAULT 1

	CONSTRAINT [PK_TrafficCondition] PRIMARY KEY ([Id])
)

GO

CREATE TRIGGER [dbo].[OnDeleteTrafficCondition]
	ON [dbo].[TrafficCondition]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE TrafficCondition SET Active = 0
		WHERE Id = (SELECT Id FROM deleted)
	END































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.