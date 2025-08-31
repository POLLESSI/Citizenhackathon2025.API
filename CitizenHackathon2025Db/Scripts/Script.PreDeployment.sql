IF OBJECT_ID('dbo.Suggestion','U') IS NOT NULL
BEGIN
    PRINT 'Dropping dbo.Suggestion (pre-deployment)…';
    DROP TABLE dbo.Suggestion;
END