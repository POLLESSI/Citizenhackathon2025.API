CREATE INDEX [IX_Suggestion_User_Active_Date]
ON [dbo].[Suggestion]([User_Id], [DateSuggestion] DESC)
INCLUDE ([Active])
WHERE [Active] = 1;

CREATE INDEX [IX_Suggestion_EventId]
ON [dbo].[Suggestion]([EventId])
WHERE [EventId] IS NOT NULL;

CREATE INDEX [IX_Suggestion_ForecastId]
ON [dbo].[Suggestion]([ForecastId])
WHERE [ForecastId] IS NOT NULL;

CREATE INDEX [IX_Suggestion_TrafficId]
ON [dbo].[Suggestion]([TrafficId])
WHERE [TrafficId] IS NOT NULL;


























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.