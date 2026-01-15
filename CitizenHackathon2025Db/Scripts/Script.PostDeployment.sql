/* ===================================================================
   Post-Deployment (idempotent) — CitizenHackathon2025
   Does NOT create databases or tables. Only takes care of:
   - Index WeatherForecast
   - Trigger OnDeleteWeatherForecast (create if missing)
   - Colonnes/seed TokenHash/TokenSalt de RefreshTokensC:\Users\Pol PC\Downloads\CitizeHackathon2025V3.API\CitizenHackathon2025Db\Programmability\Views
   - Index/contrainte GptInteractions (cleaning + filtered index)
   - Procédures d’UPSERT (CREATE OR ALTER)
   - Procédures d’archivage (CREATE OR ALTER) — corrected
   =================================================================== */

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
-- RefreshTokens : Colonnes TokenHash / TokenSalt (idempotent)
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
-- (Optional) When the app no ​​longer uses the Token column in clear:
--IF COL_LENGTH('dbo.RefreshTokens','Token') IS NOT NULL
--    ALTER TABLE [dbo].[RefreshTokens] DROP COLUMN [Token];
--GO

--------------------------------------------------------------
-- GptInteractions : cleaning + indexes (idempotent)
--------------------------------------------------------------
-- Drop l'index s'il est déjà là (double création)
IF EXISTS (SELECT 1 FROM sys.indexes 
           WHERE name = N'IX_GptInteractions_Active'
             AND object_id = OBJECT_ID(N'dbo.GptInteractions'))
    DROP INDEX [IX_GptInteractions_Active] ON dbo.GptInteractions;
GO

-- Supprime l'UNIQUE "global" sur PromptHash si présent (on veut l’unicité FILTRÉE)
DECLARE @uniq sysname;
SELECT TOP(1) @uniq = i.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.GptInteractions')
  AND i.is_unique = 1 AND i.has_filter = 0 AND c.name = N'PromptHash';

IF @uniq IS NOT NULL
    EXEC(N'DROP INDEX [' + @uniq + N'] ON dbo.GptInteractions;');
GO

-- Recrée proprement, idempotent
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_GptInteractions_Active'
                 AND object_id = OBJECT_ID(N'dbo.GptInteractions'))
    CREATE INDEX [IX_GptInteractions_Active]
      ON dbo.GptInteractions([Active]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'UX_GptInteractions_Active_PromptHash'
                 AND object_id = OBJECT_ID(N'dbo.GptInteractions'))
    CREATE UNIQUE INDEX [UX_GptInteractions_Active_PromptHash]
      ON dbo.GptInteractions([PromptHash])
      WHERE [Active] = 1;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_GptInteractions_Active'
      AND [object_id] = OBJECT_ID(N'dbo.GptInteractions')
)
    CREATE INDEX [IX_GptInteractions_Active]
    ON dbo.GptInteractions([Active]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'UX_GptInteractions_Active_PromptHash'
      AND [object_id] = OBJECT_ID(N'dbo.GptInteractions')
)
    CREATE UNIQUE INDEX [UX_GptInteractions_Active_PromptHash]
    ON dbo.GptInteractions([PromptHash])
    WHERE [Active] = 1;GO

/* ========================
   Procedures: UPSERTS
   ======================== */

-- dbo.CrowdCalendar_Upsert
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

-- dbo.sp_CrowdInfo_Upsert
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

-- dbo.sp_GptInteraction_Upsert
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

-- dbo.sp_TrafficCondition_Upsert
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

-- dbo.sp_WeatherForecast_Upsert
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

-- dbo.sp_ArchivePastCrowdInfo
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

-- dbo.sp_ArchivePastGptInteraction  (⚠ corrects table → GptInteractions)
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

-- dbo.sp_ArchivePastTrafficCondition
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

-- dbo.sp_ArchivePastWeatherForecast (⚠ corrects table → WeatherForecast)
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

-- Recreate IsExpired without PERSISTED if it was created with PERSISTED or set incorrectly
IF OBJECT_ID('dbo.UserSessions','U') IS NOT NULL
BEGIN
    DECLARE @isComputed INT = COLUMNPROPERTY(OBJECT_ID('dbo.UserSessions'),'IsExpired','IsComputed');
    IF @isComputed = 1
    BEGIN
        -- Drops and recreates the computed column (ALTER COLUMN is not supported on computed columns)
        ALTER TABLE dbo.UserSessions DROP COLUMN IsExpired;
        ALTER TABLE dbo.UserSessions
          ADD IsExpired AS (CASE WHEN ExpiresAtUtc <= SYSUTCDATETIME() THEN 1 ELSE 0 END);
    END
END
GO

IF OBJECT_ID('dbo.TrafficCondition','U') IS NULL
BEGIN
    PRINT 'dbo.TrafficCondition missing - skip migration.';
    RETURN;
END
GO

-- 1) Add missing columns (safe)
IF COL_LENGTH('dbo.TrafficCondition','Provider') IS NULL
    ALTER TABLE dbo.TrafficCondition ADD Provider NVARCHAR(16) NULL;
IF COL_LENGTH('dbo.TrafficCondition','ExternalId') IS NULL
    ALTER TABLE dbo.TrafficCondition ADD ExternalId NVARCHAR(128) NULL;
IF COL_LENGTH('dbo.TrafficCondition','Fingerprint') IS NULL
    ALTER TABLE dbo.TrafficCondition ADD Fingerprint VARBINARY(32) NULL;
IF COL_LENGTH('dbo.TrafficCondition','LastSeenAt') IS NULL
    ALTER TABLE dbo.TrafficCondition ADD LastSeenAt DATETIME2(0) NULL;
GO

-- 2) Add defaults (safe)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_TrafficCondition_Provider')
    ALTER TABLE dbo.TrafficCondition
    ADD CONSTRAINT DF_TrafficCondition_Provider DEFAULT('odwb') FOR Provider;

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_TrafficCondition_LastSeenAt')
    ALTER TABLE dbo.TrafficCondition
    ADD CONSTRAINT DF_TrafficCondition_LastSeenAt DEFAULT (SYSUTCDATETIME()) FOR LastSeenAt;
GO

-- 3) Backfill BEFORE NOT NULL
UPDATE dbo.TrafficCondition SET Provider='legacy' WHERE Provider IS NULL;
UPDATE dbo.TrafficCondition SET ExternalId=CONCAT('legacy-', Id) WHERE ExternalId IS NULL;
UPDATE dbo.TrafficCondition SET Fingerprint=HASHBYTES('SHA2_256', CONCAT('legacy|', Id)) WHERE Fingerprint IS NULL;
UPDATE dbo.TrafficCondition SET LastSeenAt=SYSUTCDATETIME() WHERE LastSeenAt IS NULL;
GO

-- 4) Now enforce NOT NULL
ALTER TABLE dbo.TrafficCondition ALTER COLUMN Provider NVARCHAR(16) NOT NULL;
ALTER TABLE dbo.TrafficCondition ALTER COLUMN ExternalId NVARCHAR(128) NOT NULL;
ALTER TABLE dbo.TrafficCondition ALTER COLUMN Fingerprint VARBINARY(32) NOT NULL;
ALTER TABLE dbo.TrafficCondition ALTER COLUMN LastSeenAt DATETIME2(0) NOT NULL;
GO









































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.