IF OBJECT_ID(N'dbo.Suggestion', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_Suggestion_User_Active_Date'
          AND object_id = OBJECT_ID(N'dbo.Suggestion')
    )
    BEGIN
        CREATE INDEX [IX_Suggestion_User_Active_Date]
        ON [dbo].[Suggestion]([User_Id], [DateSuggestion] DESC)
        INCLUDE ([Active])
        WHERE [Active] = 1;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_Suggestion_EventId'
          AND object_id = OBJECT_ID(N'dbo.Suggestion')
    )
    BEGIN
        CREATE INDEX [IX_Suggestion_EventId]
        ON [dbo].[Suggestion]([EventId])
        WHERE [EventId] IS NOT NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_Suggestion_ForecastId'
          AND object_id = OBJECT_ID(N'dbo.Suggestion')
    )
    BEGIN
        CREATE INDEX [IX_Suggestion_ForecastId]
        ON [dbo].[Suggestion]([ForecastId])
        WHERE [ForecastId] IS NOT NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_Suggestion_TrafficId'
          AND object_id = OBJECT_ID(N'dbo.Suggestion')
    )
    BEGIN
        CREATE INDEX [IX_Suggestion_TrafficId]
        ON [dbo].[Suggestion]([TrafficId])
        WHERE [TrafficId] IS NOT NULL;
    END;
END;
GO













































































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.