CREATE TABLE [dbo].[TrafficCondition]
(
	[Id] INT IDENTITY,
	[Latitude] DECIMAL(9, 2),
	[Longitude] DECIMAL(9, 3),
	[DateCondition] DATETIME2(0),
	[CongestionLevel] NVARCHAR(16),
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
		WHERE Id IN (SELECT Id FROM deleted)
	END
GO

CREATE INDEX IX_TrafficCondition_Active_DateCondition
ON dbo.TrafficCondition (Active, DateCondition DESC);
GO

CREATE UNIQUE INDEX UX_TrafficCondition_Active_LatLon
ON dbo.TrafficCondition(Latitude, Longitude)
WHERE Active = 1;
GO






























































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.