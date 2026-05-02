CREATE TABLE [dbo].[CrowdSafetyAlert]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,

    [AntennaId] INT NOT NULL,
    [EventId] INT NULL,

    [Severity] TINYINT NOT NULL,
    [Status] NVARCHAR(32) NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_Status] DEFAULT N'PendingValidation',

    [ActiveConnections] INT NOT NULL,
    [UniqueDevices] INT NOT NULL,
    [BaselineConnections] INT NULL,

    [IsRural] BIT NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_IsRural] DEFAULT 0,
    [IsNight] BIT NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_IsNight] DEFAULT 0,
    [IsKnownEvent] BIT NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_IsKnownEvent] DEFAULT 0,
    [IsSensitiveZone] BIT NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_IsSensitiveZone] DEFAULT 0,

    [Latitude] DECIMAL(9,6) NOT NULL,
    [Longitude] DECIMAL(9,6) NOT NULL,

    [Title] NVARCHAR(128) NOT NULL,
    [Message] NVARCHAR(512) NOT NULL,

    [DetectedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_DetectedAtUtc] DEFAULT SYSUTCDATETIME(),
    [ValidatedAtUtc] DATETIME2(3) NULL,
    [ValidatedByUserId] INT NULL,
    [LastReminderAtUtc] DATETIME2(3) NULL,
    [ReminderCount] INT NOT NULL CONSTRAINT DF_CrowdSafetyAlert_ReminderCount DEFAULT 0,

    [Active] BIT NOT NULL CONSTRAINT [DF_CrowdSafetyAlert_Active] DEFAULT 1,

    CONSTRAINT [PK_CrowdSafetyAlert] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_CrowdSafetyAlert_Antenna] FOREIGN KEY ([AntennaId]) REFERENCES [dbo].[CrowdInfoAntenna]([Id]),

    CONSTRAINT [CK_CrowdSafetyAlert_Severity] CHECK ([Severity] BETWEEN 1 AND 4)
);
GO

CREATE INDEX [IX_CrowdSafetyAlert_Latest]
ON [dbo].[CrowdSafetyAlert] ([DetectedAtUtc] DESC)
INCLUDE ([AntennaId], [Severity], [Status], [ActiveConnections], [UniqueDevices], [Active]);
GO

CREATE INDEX [IX_CrowdSafetyAlert_Antenna_Recent]
ON [dbo].[CrowdSafetyAlert] ([AntennaId], [DetectedAtUtc] DESC)
INCLUDE ([Severity], [Status], [Active]);
GO

CREATE INDEX [IX_CrowdSafetyAlert_Active_Status]
ON [dbo].[CrowdSafetyAlert] ([Active], [Status], [Severity], [DetectedAtUtc] DESC);
GO










































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.