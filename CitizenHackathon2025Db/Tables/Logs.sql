CREATE TABLE [dbo].[Logs]
(
	[Id] BIGINT IDENTITY,
	TimeStamp     DATETIME2(3)   NOT NULL,
    Level         NVARCHAR(16)   NOT NULL,         -- Information, Warning, Error...
    Message       NVARCHAR(MAX)  NOT NULL,
    MessageTemplate NVARCHAR(MAX) NULL,
    Exception     NVARCHAR(MAX)  NULL,
    SourceContext NVARCHAR(256)  NULL,             
    RequestPath   NVARCHAR(512)  NULL,            
    RequestId     NVARCHAR(128)  NULL,             
    UserName      NVARCHAR(256)  NULL,             
    Properties    NVARCHAR(MAX)  NULL

    CONSTRAINT [PK_Logs] PRIMARY KEY ([Id])
);

GO

CREATE INDEX IX_Logs_TimeStamp       ON dbo.Logs (TimeStamp DESC);

GO

CREATE INDEX IX_Logs_Level_Time      ON dbo.Logs (Level, TimeStamp DESC);

GO

CREATE INDEX IX_Logs_RequestPathTime ON dbo.Logs (RequestPath, TimeStamp DESC);








































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.