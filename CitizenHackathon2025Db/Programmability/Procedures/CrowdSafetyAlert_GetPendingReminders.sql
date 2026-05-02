CREATE PROCEDURE dbo.CrowdSafetyAlert_GetPendingReminders
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        *
    FROM dbo.CrowdSafetyAlert
    WHERE Active = 1
      AND Status = 'PendingValidation'
      AND DetectedAtUtc <= DATEADD(HOUR, -2, SYSUTCDATETIME())
      AND ISNULL(ReminderCount, 0) < 3
      AND (
            LastReminderAtUtc IS NULL
            OR LastReminderAtUtc <= DATEADD(HOUR, -2, SYSUTCDATETIME())
          )
    ORDER BY Severity DESC, DetectedAtUtc ASC;
END
GO








































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.