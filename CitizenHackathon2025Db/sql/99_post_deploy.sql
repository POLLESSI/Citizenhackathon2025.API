--sql/99_post_deploy.sql

/* ===================================================================
   Post-Deployment (idempotent) — CitizenHackathon2025
   NOTE: GPT indexes (drop/recreate + unique filtered) are managed
         in 01_fix_gpt_indexes.sql to avoid duplicates here.
   =================================================================== */

SET NOCOUNT ON;
--------------------------------------------------------------
-- WeatherForecast : Index (idempotent)
--------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_DateWeather'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_DateWeather]
        ON [dbo].[WeatherForecast]([DateWeather]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_TemperatureC'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_TemperatureC]
        ON [dbo].[WeatherForecast]([TemperatureC]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_TemperatureF'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_TemperatureF]
        ON [dbo].[WeatherForecast]([TemperatureF]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_Summary'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_Summary]
        ON [dbo].[WeatherForecast]([Summary]);
GO

--------------------------------------------------------------
-- WeatherForecast : Trigger INSTEAD OF DELETE (create if missing)
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.OnDeleteWeatherForecast', N'TR') IS NULL
    EXEC(N'
        CREATE TRIGGER [dbo].[OnDeleteWeatherForecast]
        ON [dbo].[WeatherForecast]
        INSTEAD OF DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            UPDATE WF SET Active = 0
            FROM dbo.WeatherForecast WF
            JOIN deleted d ON d.Id = WF.Id;
        END
    ');
GO

--------------------------------------------------------------
-- RefreshTokens : Colonnes TokenHash / TokenSalt + seed (idempotent)
--------------------------------------------------------------
IF COL_LENGTH('dbo.RefreshTokens','TokenHash') IS NULL
    ALTER TABLE [dbo].[RefreshTokens] ADD TokenHash VARBINARY(32) NULL;
GO
IF COL_LENGTH('dbo.RefreshTokens','TokenSalt') IS NULL
    ALTER TABLE [dbo].[RefreshTokens] ADD TokenSalt VARBINARY(16) NULL;
GO

IF EXISTS (SELECT 1 FROM [dbo].[RefreshTokens]
           WHERE TokenHash IS NULL OR TokenSalt IS NULL)
BEGIN
    UPDATE rt
    SET
      TokenSalt = ISNULL(TokenSalt, CRYPT_GEN_RANDOM(16)),
      TokenHash = ISNULL(
                    TokenHash,
                    HASHBYTES(
                        'SHA2_256',
                        CONVERT(varbinary(8000), rt.Token, 0) + ISNULL(TokenSalt, 0x)
                    )
                  )
    FROM dbo.RefreshTokens AS rt;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.RefreshTokens')
             AND name = 'TokenSalt' AND is_nullable = 1)
    ALTER TABLE [dbo].[RefreshTokens] ALTER COLUMN TokenSalt VARBINARY(16) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.RefreshTokens')
             AND name = 'TokenHash' AND is_nullable = 1)
    ALTER TABLE [dbo].[RefreshTokens] ALTER COLUMN TokenHash VARBINARY(32) NOT NULL;
GO
-- (Optional) when the Token (clear) column is no longer used:
--IF COL_LENGTH('dbo.RefreshTokens','Token') IS NOT NULL
--    ALTER TABLE [dbo].[RefreshTokens] DROP COLUMN [Token];
--GO

/* ========================
   Procédures: UPSERTS
   ======================== */
--------------------------------------------------------------
-- dbo.CrowdCalendar_Upsert
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrowdCalendar_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.CrowdCalendar_Upsert AS SELECT 1');
GO
ALTER PROCEDURE dbo.CrowdCalendar_Upsert
    @DateUtc          date,
    @RegionCode       nvarchar(32),
    @PlaceId          int            = NULL,
    @EventName        nvarchar(128)  = NULL,
    @ExpectedLevel    tinyint,
    @Confidence       tinyint        = NULL,
    @StartLocalTime   time(0)        = NULL,
    @EndLocalTime     time(0)        = NULL,
    @LeadHours        int            = 3,
    @MessageTemplate  nvarchar(512)  = NULL,
    @Tags             nvarchar(128)  = NULL,
    @Active           bit            = 1
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.CrowdCalendar AS tgt
    USING (SELECT @DateUtc AS DateUtc, @RegionCode AS RegionCode, @PlaceId AS PlaceId) AS src
       ON (tgt.DateUtc = src.DateUtc
           AND tgt.RegionCode = src.RegionCode
           AND ((tgt.PlaceId IS NULL AND src.PlaceId IS NULL) OR tgt.PlaceId = src.PlaceId)
           AND tgt.Active = 1)
    WHEN MATCHED THEN
        UPDATE SET
            EventName       = @EventName,
            ExpectedLevel   = @ExpectedLevel,
            Confidence      = @Confidence,
            StartLocalTime  = @StartLocalTime,
            EndLocalTime    = @EndLocalTime,
            LeadHours       = @LeadHours,
            MessageTemplate = @MessageTemplate,
            Tags            = @Tags,
            Active          = @Active
    WHEN NOT MATCHED THEN
        INSERT (DateUtc, RegionCode, PlaceId, EventName, ExpectedLevel, Confidence,
                StartLocalTime, EndLocalTime, LeadHours, MessageTemplate, Tags, Active)
        VALUES (@DateUtc, @RegionCode, @PlaceId, @EventName, @ExpectedLevel, @Confidence,
                @StartLocalTime, @EndLocalTime, @LeadHours, @MessageTemplate, @Tags, @Active);
END
GO

--------------------------------------------------------------
-- dbo.sp_CrowdInfo_Upsert
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_CrowdInfo_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_CrowdInfo_Upsert AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_CrowdInfo_Upsert
    @LocationName NVARCHAR(64),
    @Latitude     DECIMAL(9,6),
    @Longitude    DECIMAL(9,6),
    @CrowdLevel   INT,
    @Timestamp    DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    DELETE FROM dbo.CrowdInfo WITH (ROWLOCK)
     WHERE Active = 1
       AND LocationName = @LocationName
       AND Latitude     = @Latitude
       AND Longitude    = @Longitude;

    INSERT INTO dbo.CrowdInfo (LocationName, Latitude, Longitude, CrowdLevel, [Timestamp], Active)
    VALUES (@LocationName, @Latitude, @Longitude, @CrowdLevel, @Timestamp, 1);

    DECLARE @NewId INT = SCOPE_IDENTITY();
    COMMIT;

    SELECT * FROM dbo.CrowdInfo WHERE Id = @NewId;
END
GO

--------------------------------------------------------------
-- dbo.sp_GptInteraction_Upsert
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_GptInteraction_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_GptInteraction_Upsert AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_GptInteraction_Upsert
    @Prompt      NVARCHAR(MAX),
    @PromptHash  NVARCHAR(64),
    @Response    NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    DELETE FROM dbo.GptInteractions WITH (ROWLOCK)
     WHERE Active = 1
       AND PromptHash = @PromptHash;

    INSERT INTO dbo.GptInteractions (Prompt, PromptHash, Response, CreatedAt, Active)
    VALUES (@Prompt, @PromptHash, @Response, SYSUTCDATETIME(), 1);

    DECLARE @NewId INT = SCOPE_IDENTITY();
    COMMIT;

    SELECT * FROM dbo.GptInteractions WHERE Id = @NewId;
END
GO

--------------------------------------------------------------
-- dbo.sp_TrafficCondition_Upsert
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_TrafficCondition_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_TrafficCondition_Upsert AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_TrafficCondition_Upsert
    @Latitude        DECIMAL(9, 2),
    @Longitude       DECIMAL(9, 3),
    @DateCondition   DATETIME2(0),
    @CongestionLevel NVARCHAR(16),
    @IncidentType    NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TrafficCondition
       SET Active = 0
     WHERE Active = 1
       AND Latitude  = @Latitude
       AND Longitude = @Longitude;

    INSERT INTO dbo.TrafficCondition
        (Latitude, Longitude, DateCondition, CongestionLevel, IncidentType, Active)
    OUTPUT INSERTED.* 
    VALUES
        (@Latitude, @Longitude, @DateCondition, @CongestionLevel, @IncidentType, 1);
END
GO

--------------------------------------------------------------
-- dbo.sp_WeatherForecast_Upsert
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_WeatherForecast_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_WeatherForecast_Upsert AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_WeatherForecast_Upsert
    @DateWeather    DATETIME2,
    @Latitude       DECIMAL(9,6),
    @Longitude      DECIMAL(9,6),
    @TemperatureC   INT,
    @Summary        NVARCHAR(256),
    @RainfallMm     FLOAT          = NULL,
    @Humidity       INT            = NULL,
    @WindSpeedKmh   FLOAT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WeatherForecast
       SET Active = 0
     WHERE Active = 1
       AND DateWeather = @DateWeather
       AND Latitude    = @Latitude
       AND Longitude   = @Longitude;

    INSERT INTO dbo.WeatherForecast
        (DateWeather, Latitude, Longitude, TemperatureC, Summary, RainfallMm, Humidity, WindSpeedKmh, Active)
    OUTPUT INSERTED.* 
    VALUES
        (@DateWeather, @Latitude, @Longitude, @TemperatureC, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh, 1);
END
GO

/* ========================
   Procedures: Archiving
   ======================== */
--------------------------------------------------------------
-- dbo.sp_ArchivePastCrowdInfo
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_ArchivePastCrowdInfo', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastCrowdInfo AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_ArchivePastCrowdInfo
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[CrowdInfo]
      SET [Active] = 0
    WHERE [Active] = 1
      AND [Timestamp] < DATEADD(DAY, -1, SYSUTCDATETIME());
END
GO

--------------------------------------------------------------
-- dbo.sp_ArchivePastGptInteraction
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_ArchivePastGptInteraction', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastGptInteraction AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_ArchivePastGptInteraction
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[GptInteractions]
      SET [Active] = 0,
          [DateDeleted] = ISNULL([DateDeleted], SYSUTCDATETIME())
    WHERE [Active] = 1
      AND [CreatedAt] < DATEADD(DAY, -1, SYSUTCDATETIME());
END
GO

--------------------------------------------------------------
-- dbo.sp_ArchivePastTrafficCondition
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_ArchivePastTrafficCondition', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastTrafficCondition AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_ArchivePastTrafficCondition
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[TrafficCondition]
      SET [Active] = 0
    WHERE [Active] = 1
      AND [DateCondition] < DATEADD(DAY, -1, SYSUTCDATETIME());
END
GO

--------------------------------------------------------------
-- dbo.sp_ArchivePastWeatherForecast
--------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_ArchivePastWeatherForecast', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastWeatherForecast AS SELECT 1');
GO
ALTER PROCEDURE dbo.sp_ArchivePastWeatherForecast
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[WeatherForecast]
      SET [Active] = 0
    WHERE [Active] = 1
      AND [DateWeather] < DATEADD(DAY, -1, SYSUTCDATETIME());
END
GO

/* ========================
   UserSessions : IsExpired computed (non PERSISTED)
   ======================== */
IF OBJECT_ID('dbo.UserSessions','U') IS NOT NULL
BEGIN
    DECLARE @isComputed INT = COLUMNPROPERTY(OBJECT_ID('dbo.UserSessions'),'IsExpired','IsComputed');
    IF @isComputed = 1
    BEGIN
        -- we (re)create the calculated column WITHOUT PERSISTED to avoid error 4936
        ALTER TABLE dbo.UserSessions DROP COLUMN IsExpired;
        ALTER TABLE dbo.UserSessions
          ADD IsExpired AS (CASE WHEN ExpiresAtUtc <= SYSUTCDATETIME() THEN 1 ELSE 0 END);
    END
END
GO





































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.