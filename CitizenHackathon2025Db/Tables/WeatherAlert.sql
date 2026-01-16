CREATE TABLE dbo.WeatherAlert
(
    Id           INT IDENTITY(1,1) NOT NULL,
    Provider     NVARCHAR(16)  NOT NULL,  -- "openweather"
    ExternalId   NVARCHAR(128) NOT NULL,  -- stable key
    Latitude     DECIMAL(9,6)  NULL,
    Longitude    DECIMAL(9,6)  NULL,
    SenderName   NVARCHAR(128) NULL,
    EventName    NVARCHAR(128) NULL,
    StartUtc     DATETIME2(0)  NOT NULL,
    EndUtc       DATETIME2(0)  NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    Tags         NVARCHAR(512) NULL,      -- comma-join
    Severity     TINYINT NULL,
    LastSeenAt   DATETIME2(0) NOT NULL,
    Active       BIT NOT NULL CONSTRAINT DF_WeatherAlert_Active DEFAULT(1),

    CONSTRAINT PK_WeatherAlert PRIMARY KEY (Id),
    CONSTRAINT UQ_WeatherAlert_Provider_ExternalId UNIQUE (Provider, ExternalId)
);

GO

CREATE INDEX IX_WeatherAlert_Provider_Active_StartUtc
ON dbo.WeatherAlert(Provider, Active, StartUtc DESC);

GO


































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.