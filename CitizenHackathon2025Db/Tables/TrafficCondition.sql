CREATE TABLE [dbo].[TrafficCondition]
(
	[Id] INT IDENTITY,
	[Latitude] DECIMAL(8, 2),
	[Longitude] DECIMAL(9, 3),
	[DateCondition] DATE,
	[CongestionLevel] NVARCHAR(2),
	[IncidentType] NVARCHAR(64),
	[Active] BIT DEFAULT 1

	CONSTRAINT [PK_TrafficCondition] PRIMARY KEY ([Id]),
	--CONSTRAINT [UQ_TrafficCondition_Latitude_Longitude_DateCondition] UNIQUE ([Latitude], [Longitude], [DateCondition])
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
