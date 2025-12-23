CREATE TABLE [dbo].[Suggestion]
(
    [Id]                   INT IDENTITY,
    [User_Id]              INT NOT NULL,
    [DateSuggestion]       DATETIME2(0) NOT NULL CONSTRAINT [DF_Suggestion_DateSuggestion] DEFAULT (SYSUTCDATETIME()),
    [OriginalPlace]        NVARCHAR(128) NULL,
    [SuggestedAlternatives] NVARCHAR(MAX) NULL,
    [Reason]               NVARCHAR(MAX) NULL,
    [Active]               BIT NOT NULL CONSTRAINT [DF_Suggestion_Active] DEFAULT (1),
    [DateDeleted]          DATETIME2(0) NULL,
    [CrowdId]              INT NULL,
    [EventId]              INT NULL,
    [PlaceId]              INT NULL,
    [TrafficId]            INT NULL,
    [ForecastId]           INT NULL,
    [LocationName]         NVARCHAR(128) NULL

    CONSTRAINT [PK_Suggestion] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Suggestion_User] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Suggestion_Crowd] FOREIGN KEY ([CrowdId]) REFERENCES [dbo].[CrowdInfo]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Event]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_Place] FOREIGN KEY ([PlaceId]) REFERENCES [dbo].[Place]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_Traffic] FOREIGN KEY ([TrafficId]) REFERENCES [dbo].[TrafficCondition]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_WeatherForecast] FOREIGN KEY ([ForecastId]) REFERENCES [dbo].[WeatherForecast]([Id]) ON DELETE SET NULL,
);

GO
CREATE INDEX IX_Suggestion_Active_DateSuggestion
ON dbo.Suggestion (Active, DateSuggestion DESC);

GO
CREATE TRIGGER [dbo].[OnDeleteSuggestion]
    ON [dbo].[Suggestion]
    INSTEAD OF DELETE
    AS
    BEGIN
        UPDATE Suggestion 
        SET Active = 0,
            DateDeleted = SYSUTCDATETIME()
        WHERE Id IN (SELECT Id FROM deleted)
    END
GO

CREATE INDEX IX_Suggestion_OriginalPlace ON dbo.Suggestion (OriginalPlace);
GO


















































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.