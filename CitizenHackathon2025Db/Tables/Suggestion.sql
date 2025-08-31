CREATE TABLE [dbo].[Suggestion]
(
    [Id]                   INT IDENTITY(1,1) CONSTRAINT [PK_Suggestion] PRIMARY KEY,
    [User_Id]              INT NOT NULL,
    [DateSuggestion]       DATETIME2(0) NOT NULL CONSTRAINT [DF_Suggestion_DateSuggestion] DEFAULT (SYSUTCDATETIME()),
    [OriginalPlace]        NVARCHAR(128) NULL,
    [SuggestedAlternatives] NVARCHAR(MAX) NULL,
    [Reason]               NVARCHAR(MAX) NULL,
    [Active]               BIT NOT NULL CONSTRAINT [DF_Suggestion_Active] DEFAULT (1),
    [DateDeleted]          DATETIME2(0) NULL,
    [CrowdId]              INT NULL,
    [EventId]              INT NULL,
    [TrafficId]            INT NULL,
    [ForecastId]           INT NULL,
   
    [LocationName]         NVARCHAR(128) NULL,
    CONSTRAINT [FK_Suggestion_User]            FOREIGN KEY ([User_Id])     REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Suggestion_Crowd]           FOREIGN KEY ([CrowdId])     REFERENCES [dbo].[CrowdInfo]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_Event]           FOREIGN KEY ([EventId])     REFERENCES [dbo].[Event]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_Traffic]         FOREIGN KEY ([TrafficId])   REFERENCES [dbo].[TrafficCondition]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Suggestion_WeatherForecast] FOREIGN KEY ([ForecastId])  REFERENCES [dbo].[WeatherForecast]([Id]) ON DELETE SET NULL,
);


















































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.