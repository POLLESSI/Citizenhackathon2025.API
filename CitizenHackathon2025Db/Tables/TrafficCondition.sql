CREATE TABLE [dbo].[TrafficCondition]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Latitude] DECIMAL(9, 6) NOT NULL,
    [Longitude] DECIMAL(9, 6) NOT NULL,
    [DateCondition] DATETIME2(0) NOT NULL,
    [CongestionLevel] NVARCHAR(16) NOT NULL,
    [IncidentType] NVARCHAR(64) NOT NULL,

    [Provider] NVARCHAR(16) NOT NULL CONSTRAINT DF_TrafficCondition_Provider DEFAULT('odwb'),
    [ExternalId] NVARCHAR(128) NOT NULL,
    [Fingerprint] VARBINARY(32) NOT NULL,
    [LastSeenAt] DATETIME2(0) NOT NULL CONSTRAINT DF_TrafficCondition_LastSeenAt DEFAULT (SYSUTCDATETIME()),

    [Title] NVARCHAR(256) NULL,
    [Road] NVARCHAR(128) NULL,
    [Severity] TINYINT NULL,
    [GeomWkt] NVARCHAR(MAX) NULL,

    [Active] BIT NOT NULL CONSTRAINT DF_TrafficCondition_Active DEFAULT(1),

    CONSTRAINT [PK_TrafficCondition] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_TrafficCondition_ExternalId_Provider] UNIQUE ([ExternalId], [Provider])
);
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

--CREATE UNIQUE INDEX UX_TrafficCondition_Active_LatLon
--ON dbo.TrafficCondition(Latitude, Longitude)
--WHERE Active = 1;
--GO






























































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.