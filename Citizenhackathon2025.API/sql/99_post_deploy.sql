/* ===================================================================
   Post-Deployment (idempotent) — CitizenHackathon2025
   Scope:
   - WeatherForecast indexes
   - WeatherForecast soft-delete trigger
   - RefreshTokens TokenHash / TokenSalt migration
   - GptInteractions cleanup + filtered indexes
   - CrowdCalendar procedures + constraints
   - Other UPSERT procedures
   - Archiving procedures
   - TrafficCondition migration
   - UserSessions computed column fix
   Notes:
   - Does NOT create databases
   - Does NOT create base tables
   - Safe for republication
   =================================================================== */

SET NOCOUNT ON;
GO

/* ==========================================================
   WeatherForecast : indexes
   ========================================================== */
IF OBJECT_ID(N'dbo.WeatherForecast', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_WeatherForecast_DateWeather'
          AND object_id = OBJECT_ID(N'dbo.WeatherForecast')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_WeatherForecast_DateWeather]
            ON [dbo].[WeatherForecast]([DateWeather]);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_WeatherForecast_TemperatureC'
          AND object_id = OBJECT_ID(N'dbo.WeatherForecast')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_WeatherForecast_TemperatureC]
            ON [dbo].[WeatherForecast]([TemperatureC]);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_WeatherForecast_TemperatureF'
          AND object_id = OBJECT_ID(N'dbo.WeatherForecast')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_WeatherForecast_TemperatureF]
            ON [dbo].[WeatherForecast]([TemperatureF]);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_WeatherForecast_Summary'
          AND object_id = OBJECT_ID(N'dbo.WeatherForecast')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_WeatherForecast_Summary]
            ON [dbo].[WeatherForecast]([Summary]);
    END;
END;
GO

/* ==========================================================
   WeatherForecast : INSTEAD OF DELETE trigger (soft delete)
   ========================================================== */
IF OBJECT_ID(N'dbo.WeatherForecast', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.OnDeleteWeatherForecast', N'TR') IS NULL
        EXEC(N'
            CREATE TRIGGER [dbo].[OnDeleteWeatherForecast]
            ON [dbo].[WeatherForecast]
            INSTEAD OF DELETE
            AS
            BEGIN
                SET NOCOUNT ON;

                UPDATE WF
                SET Active = 0
                FROM dbo.WeatherForecast WF
                INNER JOIN deleted d ON d.Id = WF.Id;
            END
        ');
END;
GO

/* ==========================================================
   RefreshTokens : TokenHash / TokenSalt migration
   ========================================================== */
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.RefreshTokens', 'TokenHash') IS NULL
        ALTER TABLE dbo.RefreshTokens ADD TokenHash VARBINARY(32) NULL;

    IF COL_LENGTH('dbo.RefreshTokens', 'TokenSalt') IS NULL
        ALTER TABLE dbo.RefreshTokens ADD TokenSalt VARBINARY(16) NULL;
END;
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM dbo.RefreshTokens
        WHERE TokenHash IS NULL
           OR TokenSalt IS NULL
    )
    BEGIN
        ;WITH RT AS
        (
            SELECT Id, Token, TokenHash, TokenSalt
            FROM dbo.RefreshTokens
        )
        UPDATE rt
        SET
            TokenSalt = ISNULL(rt.TokenSalt, CRYPT_GEN_RANDOM(16)),
            TokenHash = ISNULL(
                rt.TokenHash,
                HASHBYTES(
                    'SHA2_256',
                    CONVERT(VARBINARY(8000), rt.Token, 0)
                    + ISNULL(rt.TokenSalt, 0x)
                )
            )
        FROM dbo.RefreshTokens rt;
    END;
END;
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens')
          AND name = N'TokenSalt'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.RefreshTokens ALTER COLUMN TokenSalt VARBINARY(16) NOT NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens')
          AND name = N'TokenHash'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.RefreshTokens ALTER COLUMN TokenHash VARBINARY(32) NOT NULL;
    END;
END;
GO

/* ==========================================================
   GptInteractions : cleanup + indexes
   ========================================================== */
IF OBJECT_ID(N'dbo.GptInteractions', N'U') IS NOT NULL
BEGIN
    DECLARE @uniq sysname;

    SELECT TOP (1) @uniq = i.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
       AND ic.index_id  = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.GptInteractions')
      AND i.is_unique = 1
      AND i.has_filter = 0
      AND c.name = N'PromptHash';

    IF @uniq IS NOT NULL
        EXEC(N'DROP INDEX [' + @uniq + N'] ON dbo.GptInteractions;');

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_GptInteractions_Active'
          AND object_id = OBJECT_ID(N'dbo.GptInteractions')
    )
    BEGIN
        CREATE INDEX [IX_GptInteractions_Active]
            ON dbo.GptInteractions([Active]);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_GptInteractions_Active_PromptHash'
          AND object_id = OBJECT_ID(N'dbo.GptInteractions')
    )
    BEGIN
        CREATE UNIQUE INDEX [UX_GptInteractions_Active_PromptHash]
            ON dbo.GptInteractions([PromptHash])
            WHERE [Active] = 1;
    END;
END;
GO

/* ==========================================================
   CrowdCalendar : procedures
   ========================================================== */
IF OBJECT_ID(N'dbo.CrowdCalendar_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.CrowdCalendar_Upsert AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.CrowdCalendar_Upsert
    @DateUtc          DATE,
    @RegionCode       NVARCHAR(32),
    @PlaceId          INT           = NULL,
    @EventName        NVARCHAR(128) = NULL,
    @ExpectedLevel    TINYINT,
    @Confidence       TINYINT       = NULL,
    @Latitude         DECIMAL(9,6)  = NULL,
    @Longitude        DECIMAL(9,6)  = NULL,
    @StartLocalTime   TIME(0)       = NULL,
    @EndLocalTime     TIME(0)       = NULL,
    @LeadHours        INT           = 3,
    @MessageTemplate  NVARCHAR(512) = NULL,
    @Tags             NVARCHAR(128) = NULL,
    @Active           BIT           = 1
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.CrowdCalendar AS tgt
    USING
    (
        SELECT
            @DateUtc    AS DateUtc,
            @RegionCode AS RegionCode,
            @PlaceId    AS PlaceId
    ) AS src
    ON (
        tgt.DateUtc = src.DateUtc
        AND tgt.RegionCode = src.RegionCode
        AND (
            (tgt.PlaceId IS NULL AND src.PlaceId IS NULL)
            OR tgt.PlaceId = src.PlaceId
        )
        AND tgt.Active = 1
    )
    WHEN MATCHED THEN
        UPDATE SET
            EventName       = @EventName,
            ExpectedLevel   = @ExpectedLevel,
            Confidence      = @Confidence,
            Latitude        = COALESCE(@Latitude, tgt.Latitude),
            Longitude       = COALESCE(@Longitude, tgt.Longitude),
            StartLocalTime  = @StartLocalTime,
            EndLocalTime    = @EndLocalTime,
            LeadHours       = @LeadHours,
            MessageTemplate = @MessageTemplate,
            Tags            = @Tags,
            Active          = @Active
    WHEN NOT MATCHED THEN
        INSERT
        (
            DateUtc,
            RegionCode,
            PlaceId,
            EventName,
            ExpectedLevel,
            Confidence,
            Latitude,
            Longitude,
            StartLocalTime,
            EndLocalTime,
            LeadHours,
            MessageTemplate,
            Tags,
            Active
        )
        VALUES
        (
            @DateUtc,
            @RegionCode,
            @PlaceId,
            @EventName,
            @ExpectedLevel,
            @Confidence,
            @Latitude,
            @Longitude,
            @StartLocalTime,
            @EndLocalTime,
            @LeadHours,
            @MessageTemplate,
            @Tags,
            @Active
        );
END;
GO

IF OBJECT_ID(N'dbo.CrowdCalendar_ExpireOldEntries', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.CrowdCalendar_ExpireOldEntries AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.CrowdCalendar_ExpireOldEntries
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.CrowdCalendar
    SET Active = 0
    WHERE Active = 1
      AND DateUtc < CAST(SYSUTCDATETIME() AS DATE);
END;
GO

/* ==========================================================
   CrowdCalendar : constraints
   ========================================================== */
IF OBJECT_ID(N'dbo.CrowdCalendar', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_CrowdCalendar_ExpectedLevel'
          AND parent_object_id = OBJECT_ID(N'dbo.CrowdCalendar')
    )
    BEGIN
        ALTER TABLE dbo.CrowdCalendar
        ADD CONSTRAINT CK_CrowdCalendar_ExpectedLevel
        CHECK (ExpectedLevel BETWEEN 1 AND 4);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_CrowdCalendar_Confidence'
          AND parent_object_id = OBJECT_ID(N'dbo.CrowdCalendar')
    )
    BEGIN
        ALTER TABLE dbo.CrowdCalendar
        ADD CONSTRAINT CK_CrowdCalendar_Confidence
        CHECK (Confidence IS NULL OR Confidence BETWEEN 0 AND 100);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_CrowdCalendar_LeadHours'
          AND parent_object_id = OBJECT_ID(N'dbo.CrowdCalendar')
    )
    BEGIN
        ALTER TABLE dbo.CrowdCalendar
        ADD CONSTRAINT CK_CrowdCalendar_LeadHours
        CHECK (LeadHours >= 0);
    END;
END;
GO

/* ==========================================================
   CrowdInfo : upsert
   ========================================================== */
IF OBJECT_ID(N'dbo.sp_CrowdInfo_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_CrowdInfo_Upsert AS BEGIN SET NOCOUNT ON; END');
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
      AND Latitude = @Latitude
      AND Longitude = @Longitude;

    INSERT INTO dbo.CrowdInfo
    (
        LocationName,
        Latitude,
        Longitude,
        CrowdLevel,
        [Timestamp],
        Active
    )
    VALUES
    (
        @LocationName,
        @Latitude,
        @Longitude,
        @CrowdLevel,
        @Timestamp,
        1
    );

    DECLARE @NewId INT = SCOPE_IDENTITY();

    COMMIT;

    SELECT *
    FROM dbo.CrowdInfo
    WHERE Id = @NewId;
END;
GO

/* ==========================================================
   GptInteractions : upsert
   ========================================================== */
IF OBJECT_ID(N'dbo.sp_GptInteraction_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_GptInteraction_Upsert AS BEGIN SET NOCOUNT ON; END');
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

    INSERT INTO dbo.GptInteractions
    (
        Prompt,
        PromptHash,
        Response,
        CreatedAt,
        Active
    )
    VALUES
    (
        @Prompt,
        @PromptHash,
        @Response,
        SYSUTCDATETIME(),
        1
    );

    DECLARE @NewId INT = SCOPE_IDENTITY();

    COMMIT;

    SELECT *
    FROM dbo.GptInteractions
    WHERE Id = @NewId;
END;
GO

/* ==========================================================
   TrafficCondition : upsert
   ========================================================== */
IF OBJECT_ID(N'dbo.sp_TrafficCondition_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_TrafficCondition_Upsert AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_TrafficCondition_Upsert
    @Latitude        DECIMAL(9,2),
    @Longitude       DECIMAL(9,3),
    @DateCondition   DATETIME2(0),
    @CongestionLevel NVARCHAR(16),
    @IncidentType    NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TrafficCondition
    SET Active = 0
    WHERE Active = 1
      AND Latitude = @Latitude
      AND Longitude = @Longitude;

    INSERT INTO dbo.TrafficCondition
    (
        Latitude,
        Longitude,
        DateCondition,
        CongestionLevel,
        IncidentType,
        Active
    )
    OUTPUT INSERTED.*
    VALUES
    (
        @Latitude,
        @Longitude,
        @DateCondition,
        @CongestionLevel,
        @IncidentType,
        1
    );
END;
GO

/* ==========================================================
   WeatherForecast : upsert
   ========================================================== */
IF OBJECT_ID(N'dbo.sp_WeatherForecast_Upsert', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_WeatherForecast_Upsert AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_WeatherForecast_Upsert
    @DateWeather   DATETIME2,
    @Latitude      DECIMAL(9,6),
    @Longitude     DECIMAL(9,6),
    @TemperatureC  INT,
    @Summary       NVARCHAR(256),
    @RainfallMm    FLOAT = NULL,
    @Humidity      INT   = NULL,
    @WindSpeedKmh  FLOAT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WeatherForecast
    SET Active = 0
    WHERE Active = 1
      AND DateWeather = @DateWeather
      AND Latitude = @Latitude
      AND Longitude = @Longitude;

    INSERT INTO dbo.WeatherForecast
    (
        DateWeather,
        Latitude,
        Longitude,
        TemperatureC,
        Summary,
        RainfallMm,
        Humidity,
        WindSpeedKmh,
        Active
    )
    OUTPUT INSERTED.*
    VALUES
    (
        @DateWeather,
        @Latitude,
        @Longitude,
        @TemperatureC,
        @Summary,
        @RainfallMm,
        @Humidity,
        @WindSpeedKmh,
        1
    );
END;
GO

/* ==========================================================
   Archiving procedures
   ========================================================== */
IF OBJECT_ID(N'dbo.sp_ArchivePastCrowdInfo', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastCrowdInfo AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_ArchivePastCrowdInfo
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.CrowdInfo
    SET Active = 0
    WHERE Active = 1
      AND [Timestamp] < DATEADD(DAY, -1, SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.sp_ArchivePastGptInteraction', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastGptInteraction AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_ArchivePastGptInteraction
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.GptInteractions
    SET Active = 0,
        DateDeleted = ISNULL(DateDeleted, SYSUTCDATETIME())
    WHERE Active = 1
      AND CreatedAt < DATEADD(DAY, -1, SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.sp_ArchivePastTrafficCondition', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastTrafficCondition AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_ArchivePastTrafficCondition
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TrafficCondition
    SET Active = 0
    WHERE Active = 1
      AND DateCondition < DATEADD(DAY, -1, SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.sp_ArchivePastWeatherForecast', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.sp_ArchivePastWeatherForecast AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_ArchivePastWeatherForecast
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WeatherForecast
    SET Active = 0
    WHERE Active = 1
      AND DateWeather < DATEADD(DAY, -1, SYSUTCDATETIME());
END;
GO

/* ==========================================================
   UserSessions : recreate IsExpired computed column if needed
   ========================================================== */
IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NOT NULL
BEGIN
    DECLARE @isComputed INT;
    SELECT @isComputed = COLUMNPROPERTY(OBJECT_ID(N'dbo.UserSessions'), N'IsExpired', 'IsComputed');

    IF @isComputed = 1
    BEGIN
        ALTER TABLE dbo.UserSessions DROP COLUMN IsExpired;
        ALTER TABLE dbo.UserSessions
            ADD IsExpired AS (CASE WHEN ExpiresAtUtc <= SYSUTCDATETIME() THEN 1 ELSE 0 END);
    END;
END;
GO

/* ==========================================================
   TrafficCondition : structural migration
   ========================================================== */
IF OBJECT_ID(N'dbo.TrafficCondition', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TrafficCondition', 'Provider') IS NULL
        ALTER TABLE dbo.TrafficCondition ADD Provider NVARCHAR(16) NULL;

    IF COL_LENGTH('dbo.TrafficCondition', 'ExternalId') IS NULL
        ALTER TABLE dbo.TrafficCondition ADD ExternalId NVARCHAR(128) NULL;

    IF COL_LENGTH('dbo.TrafficCondition', 'Fingerprint') IS NULL
        ALTER TABLE dbo.TrafficCondition ADD Fingerprint VARBINARY(32) NULL;

    IF COL_LENGTH('dbo.TrafficCondition', 'LastSeenAt') IS NULL
        ALTER TABLE dbo.TrafficCondition ADD LastSeenAt DATETIME2(0) NULL;
END;
GO

IF OBJECT_ID(N'dbo.TrafficCondition', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints
        WHERE name = N'DF_TrafficCondition_Provider'
    )
    BEGIN
        ALTER TABLE dbo.TrafficCondition
        ADD CONSTRAINT DF_TrafficCondition_Provider
        DEFAULT('odwb') FOR Provider;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints
        WHERE name = N'DF_TrafficCondition_LastSeenAt'
    )
    BEGIN
        ALTER TABLE dbo.TrafficCondition
        ADD CONSTRAINT DF_TrafficCondition_LastSeenAt
        DEFAULT(SYSUTCDATETIME()) FOR LastSeenAt;
    END;
END;
GO

IF OBJECT_ID(N'dbo.TrafficCondition', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.TrafficCondition
    SET Provider = 'legacy'
    WHERE Provider IS NULL;

    UPDATE dbo.TrafficCondition
    SET ExternalId = CONCAT('legacy-', Id)
    WHERE ExternalId IS NULL;

    UPDATE dbo.TrafficCondition
    SET Fingerprint = HASHBYTES('SHA2_256', CONCAT('legacy|', Id))
    WHERE Fingerprint IS NULL;

    UPDATE dbo.TrafficCondition
    SET LastSeenAt = SYSUTCDATETIME()
    WHERE LastSeenAt IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.TrafficCondition', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.TrafficCondition')
          AND name = N'Provider'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.TrafficCondition ALTER COLUMN Provider NVARCHAR(16) NOT NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.TrafficCondition')
          AND name = N'ExternalId'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.TrafficCondition ALTER COLUMN ExternalId NVARCHAR(128) NOT NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.TrafficCondition')
          AND name = N'Fingerprint'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.TrafficCondition ALTER COLUMN Fingerprint VARBINARY(32) NOT NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.TrafficCondition')
          AND name = N'LastSeenAt'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.TrafficCondition ALTER COLUMN LastSeenAt DATETIME2(0) NOT NULL;
    END;
END;
GO