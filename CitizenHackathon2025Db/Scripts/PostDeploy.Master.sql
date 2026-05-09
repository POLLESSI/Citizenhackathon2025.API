PRINT N'POSTDEPLOY MASTER START';
GO

:r .\Script.PostDeployment.sql
GO

PRINT N'POSTDEPLOY: Crowd antenna seed';
GO

:r .\CrowdAntennaSeedScript.sql
GO

PRINT N'POSTDEPLOY MASTER END';
GO