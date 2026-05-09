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

/* ==========================================================
   Seed Place + CrowdCalendar
   ========================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'POSTDEPLOY SEED STARTED: Place + CrowdCalendar';

    IF OBJECT_ID(N'dbo.Place', N'U') IS NULL
        THROW 50001, 'Table dbo.Place introuvable.', 1;

    IF OBJECT_ID(N'dbo.CrowdCalendar', N'U') IS NULL
        THROW 50002, 'Table dbo.CrowdCalendar introuvable.', 1;

    SET IDENTITY_INSERT dbo.Place ON;

    ;WITH SourcePlace AS
    (
        SELECT *
        FROM (VALUES
            (1, N'Maison de famille', N'Culture', CAST(1 AS bit), CAST(50.467388 AS decimal(9,6)), CAST(4.871985 AS decimal(9,6)), 120, N'culture', N'WallonieEnPoche', N'place-1001', CONVERT(datetime2(3), '2026-05-05 15:13:27.820', 121), CAST(1 AS bit)),
			(2,	N'Bouffioulx', 	N'Ville Wallonne', CAST(0 AS bit), CAST(50.383927 AS decimal(9,6)), CAST(4.469693 AS decimal(9,6)), 100000, N'Bouffioulx', NULL, NULL, NULL, CAST(1 AS bit)),
			(3,	N'Vedrin', N'Ville Wallone', CAST(0 AS bit), CAST(50.490000 AS decimal(9,6)), CAST(4.831000 AS decimal(9,6)), 100000, N'Vedrin', NULL, NULL, NULL, CAST(1 AS bit)),
			(4,	N'Aiseau', N'Ville Wallone', CAST(0 AS bit), CAST(50.420000 AS decimal(9,6)), CAST(4.553000 AS decimal(9,6)), 100000, N'Aiseau', NULL, NULL, NULL, CAST(1 AS bit)),
			(5,	N'Feluy', N'Ville Wallone', CAST(0 AS bit),	CAST(50.560000 AS decimal(9,6)), CAST(4.158000 AS decimal(9,6)), 100000, N'Feluy', NULL, NULL, NULL, CAST(1 AS bit)),
			(6,	N'Philippeville', N'Ville Wallone', CAST(0 AS bit),	CAST(50.160000 AS decimal(9,6)), CAST(4.438000 AS decimal(9,6)),  100000,  N'Philippeville', NULL, NULL, NULL, CAST(1 AS bit)),
			(7, N'Tarcienne', N'Ville Wallone', CAST(0 AS bit),	CAST(50.310000 AS decimal(9,6)), CAST(4.450000 AS decimal(9,6)), 100000, N'Tarcienne', NULL, NULL, NULL, CAST(1 AS bit)),
			(8, N'Dourbes', N'Ville Wallone', CAST(0 AS bit), CAST(50.090000 AS decimal(9,6)), CAST(4.549000 AS decimal(9,6)), 100000, N'Dourbes', NULL, NULL, NULL, CAST(1 AS bit)),
			(9,	N'Pry',	N'Ville Wallone', CAST(0 AS bit), CAST(50.260000 AS decimal(9,6)), CAST(4.380000 AS decimal(9,6)), 100000, N'Pry', NULL, NULL, NULL, CAST(1 AS bit)),
			(10, N'Châtelet', N'Ville Wallone', CAST(0 AS bit),	CAST(50.410000 AS decimal(9,6)), CAST(4.441000 AS decimal(9,6)), 100000, N'Châtelet', NULL, NULL, NULL, CAST(1 AS bit)),
			(11, N'Ragnies', N'Ville Wallone', CAST(0 AS bit), CAST(50.320000 AS decimal(9,6)), CAST(4.188000 AS decimal(9,6)), 100000,	N'Ragnies', NULL, NULL, NULL, CAST(1 AS bit)),
			(12, N'Thuin', N'Ville Wallone', CAST(0 AS bit), CAST(50.320000 AS decimal(9,6)), CAST(4.146000 AS decimal(9,6)), 100000, N'Thuin', NULL, NULL, NULL, CAST(1 AS bit)),
			(13, N'Hanzinne', N'Ville Wallone', CAST(0 AS bit),	CAST(50.310000 AS decimal(9,6)), CAST(4.514000 AS decimal(9,6)), 100000, N'Hanzinne', NULL, NULL, NULL, CAST(1 AS bit)),
			(14, N'Malonne', N'Ville Wallone', CAST(0 AS bit), CAST(50.430000 AS decimal(9,6)), CAST(4.767000 AS decimal(9,6)), 100000,	N'Malonne', NULL, NULL, NULL, CAST(1 AS bit)),
			(15, N'Acoz', N'Ville Wallone', CAST(0 AS bit),	CAST(50.360000 AS decimal(9,6)), CAST(4.478000 AS decimal(9,6)), 100000, N'Acoz', NULL, NULL, NULL, CAST(1 AS bit)),
			(16, N'Biesme', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.529000 AS decimal(9,6)), 100000, N'Biesme', NULL, NULL, NULL, CAST(1 AS bit)),
			(17, N'Fromiée', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.556000 AS decimal(9,6)), 100000, N'Fromiée', NULL, NULL, NULL, CAST(1 AS bit)),
			(18, N'Gerpinnes', N'Ville Wallone', CAST(0 AS bit), CAST(50.350000 AS decimal(9,6)), CAST(4.444000 AS decimal(9,6)), 100000, N'Gerpinnes', NULL, NULL, NULL, CAST(1 AS bit)),
			(19, N'Gougnies', N'Ville Wallone', CAST(0 AS bit),	CAST(50.360000 AS decimal(9,6)), CAST(4.557000 AS decimal(9,6)), 100000, N'Gougnies', NULL, NULL, NULL, CAST(1 AS bit)),
			(20, N'Hymiée', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.519000 AS decimal(9,6)), 100000, N'Hymiée', NULL, NULL, NULL, CAST(1 AS bit)),
			(21, N'Joncret', N'Ville Wallone', CAST(0 AS bit), CAST(50.360000 AS decimal(9,6)), CAST(4.460000 AS decimal(9,6)), 100000, N'Joncret', NULL, NULL, NULL, CAST(1 AS bit)),
			(22, N'Les Flaches', N'Ville Wallone', CAST(0 AS bit), CAST(50.340000 AS decimal(9,6)), CAST(4.471000 AS decimal(9,6)), 100000, N'Les Flaches', NULL, NULL, NULL, CAST(1 AS bit)),
			(23, N'Tarcienne', N'Ville Wallone', CAST(0 AS bit), CAST(50.310000 AS decimal(9,6)), CAST(4.450000 AS decimal(9,6)), 100000, N'Tarcienne', NULL, NULL, NULL, CAST(1 AS bit)),
			(24, N'Villers-Poterie', N'Ville Wallone', CAST(0 AS bit), CAST(50.360000 AS decimal(9,6)), CAST(4.531000 AS decimal(9,6)), 100000, N'Villers-Poterie', NULL, NULL, NULL, CAST(1 AS bit)),
			(25, N'Ligny', N'Ville Wallone', CAST(0 AS bit), CAST(50.510000 AS decimal(9,6)), CAST(4.534000 AS decimal(9,6)), 100000, N'Ligny', NULL, NULL, NULL, CAST(1 AS bit)),
			(26, N'Farciennes', N'Ville Wallone', CAST(0 AS bit), CAST(50.430000 AS decimal(9,6)), CAST(4.508000 AS decimal(9,6)), 100000, N'Farciennes', NULL, NULL, NULL, CAST(1 AS bit)),
			(27, N'Daussois', N'Ville Wallone', CAST(0 AS bit), CAST(50.210000 AS decimal(9,6)), CAST(4.416000 AS decimal(9,6)), 100000, N'Daussois', NULL, NULL, NULL, CAST(1 AS bit)),
			(28, N'Mons', N'Ville Wallone', CAST(0 AS bit), CAST(50.450000 AS decimal(9,6)), CAST(3.805000 AS decimal(9,6)), 100000, N'Mons', NULL, NULL, NULL, CAST(1 AS bit)),
			(29, N'Walcourt', N'Ville Wallone', CAST(0 AS bit), CAST(50.270000 AS decimal(9,6)), CAST(4.251000 AS decimal(9,6)), 100000, N'Walcourt', NULL, NULL, NULL, CAST(1 AS bit)),
			(30, N'Boussu-lez-Walcourt', N'Ville Wallone', CAST(0 AS bit), CAST(50.210000 AS decimal(9,6)), CAST(4.287000 AS decimal(9,6)), 100000, N'Boussu-lez-W', NULL, NULL, NULL, CAST(1 AS bit)),
			(31, N'COUILLET Centre', N'Ville Wallone', CAST(0 AS bit), CAST(50.390000 AS decimal(9,6)), CAST(4.466000 AS decimal(9,6)), 100000, N'COUILLET C'	, NULL, NULL, NULL, CAST(1 AS bit)),
			(32, N'Maison-St-Gérard', N'Ville Wallone', CAST(0 AS bit), CAST(50.370000 AS decimal(9,6)), CAST(4.692000 AS decimal(9,6)), 100000, N'Maison-St-G', NULL, NULL, NULL, CAST(1 AS bit)),
			(33, N'Solre-sur-Sambre', N'Ville Wallone', CAST(0 AS bit), CAST(50.300000 AS decimal(9,6)), CAST(4.108000 AS decimal(9,6)), 100000, N'Solre-s-S', NULL, NULL, NULL, CAST(1 AS bit)),
			(34, N'Cour-sur-Heure', N'Ville Wallone', CAST(0 AS bit), CAST(50.300000 AS decimal(9,6)), CAST(4.349000 AS decimal(9,6)), 100000, N'Cour-sur-H', NULL, NULL, NULL, CAST(1 AS bit)),
			(35, N'Gourdinne', N'Ville Wallone', CAST(0 AS bit), CAST(50.290000 AS decimal(9,6)), CAST(4.433000 AS decimal(9,6)), 100000, N'Gourdinne', NULL, NULL, NULL, CAST(1 AS bit)),
			(36, N'Waterloo', N'Ville Wallone', CAST(0 AS bit), CAST(50.700000 AS decimal(9,6)), CAST(4.316000 AS decimal(9,6)), 100000, N'Waterloo', NULL, NULL, NULL, CAST(1 AS bit)),
			(37, N'Laneffe', N'Ville Wallone', CAST(0 AS bit), CAST(50.280000 AS decimal(9,6)), CAST(4.455000 AS decimal(9,6)), 100000, N'Laneffe', NULL, NULL, NULL, CAST(1 AS bit)),
			(38, N'Mettet', N'Ville Wallone', CAST(0 AS bit), CAST(50.320000 AS decimal(9,6)), CAST(4.510000 AS decimal(9,6)), 100000, N'Mettet', NULL, NULL, NULL, CAST(1 AS bit)),
			(39, N'Biesmerée', N'Ville Wallone', CAST(0 AS bit), CAST(50.290000 AS decimal(9,6)), CAST(4.633000 AS decimal(9,6)), 100000, N'Biesmerée', NULL, NULL, NULL, CAST(1 AS bit)),
			(40, N'Florennes', N'Ville Wallone', CAST(0 AS bit), CAST(50.260000 AS decimal(9,6)), CAST(4.481000 AS decimal(9,6)), 100000, N'Florennes', NULL, NULL, NULL, CAST(1 AS bit)),
			(41, N'Monceau-sur-Sambre', N'Ville Wallone', CAST(0 AS bit), CAST(50.410000 AS decimal(9,6)), CAST(4.333000 AS decimal(9,6)), 100000, N'Monceau-s-S', NULL, NULL, NULL, CAST(1 AS bit)),
			(42, N'Morialmé', N'Ville Wallone', CAST(0 AS bit), CAST(50.280000 AS decimal(9,6)), CAST(4.489000 AS decimal(9,6)), 100000, N'Morialmé', NULL, NULL, NULL, CAST(1 AS bit)),
			(43, N'Thy-le-Château', N'Ville Wallone', CAST(0 AS bit), CAST(50.290000 AS decimal(9,6)), CAST(4.391000 AS decimal(9,6)), 100000, N'Thy-le-Château'	, NULL, NULL, NULL, CAST(1 AS bit)),
			(45, N'Villers-Deux-Églises', N'Ville Wallone', CAST(0 AS bit), CAST(50.190000 AS decimal(9,6)), CAST(4.445000 AS decimal(9,6)), 100000, N'Villers-Deux-Ég', NULL, NULL, NULL, CAST(1 AS bit)),
			(46, N'Vitrival', N'Ville Wallone', CAST(0 AS bit), CAST(50.380000 AS decimal(9,6)), CAST(4.607000 AS decimal(9,6)), 100000, N'Vitrival', NULL, NULL, NULL, CAST(1 AS bit)),
			(47, N'Chamborgneau', N'Ville Wallone', CAST(0 AS bit), CAST(50.380000 AS decimal(9,6)), CAST(4.487000 AS decimal(9,6)), 100000, N'Chamborgneau', NULL, NULL, NULL, CAST(1 AS bit)),
			(48, N'Nalinnes', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.402000 AS decimal(9,6)), 100000, N'Nalinnes', NULL, NULL, NULL, CAST(1 AS bit)),
			(49, N'Somzée', N'Ville Wallone', CAST(0 AS bit), CAST(50.290000 AS decimal(9,6)), CAST(4.457000 AS decimal(9,6)), 100000, N'Somzée', NULL, NULL, NULL, CAST(1 AS bit)),
			(50, N'Thy-le-Bauduin', N'Ville Wallone', CAST(0 AS bit), CAST(50.300000 AS decimal(9,6)), CAST(4.502000 AS decimal(9,6)), 100000, N'Thy-le-Bauduin', NULL, NULL, NULL, CAST(1 AS bit)),
			(51, N'Virelles', N'Ville Wallone', CAST(0 AS bit), CAST(50.080000 AS decimal(9,6)), CAST(4.252000 AS decimal(9,6)), 100000, N'Virelles', NULL, NULL, NULL, CAST(1 AS bit)),
			(52, N'Berzée', N'Ville Wallone', CAST(0 AS bit), CAST(50.290000 AS decimal(9,6)), CAST(4.351000 AS decimal(9,6)), 100000, N'Berzée', NULL, NULL, NULL, CAST(1 AS bit)),
			(53, N'Fraire', N'Ville Wallone', CAST(0 AS bit), CAST(50.260000 AS decimal(9,6)), CAST(4.461000 AS decimal(9,6)), 100000, N'Fraire', NULL, NULL, NULL, CAST(1 AS bit)),
			(54, N'Jumet', N'Ville Wallone', CAST(0 AS bit), CAST(50.440000 AS decimal(9,6)), CAST(4.361000 AS decimal(9,6)), 100000, N'Jumet', NULL, NULL, NULL, CAST(1 AS bit)),
			(55, N'Oret', N'Ville Wallone', CAST(0 AS bit), CAST(50.300000 AS decimal(9,6)), CAST(4.590000 AS decimal(9,6)), 100000, N'Oret', NULL, NULL, NULL, CAST(1 AS bit)),
			(56, N'Rosée', N'Ville Wallone', CAST(0 AS bit), CAST(50.230000 AS decimal(9,6)), CAST(4.601000 AS decimal(9,6)), 100000, N'Rosée', NULL, NULL, NULL, CAST(1 AS bit)),
			(57, N'Saint-Aubin', N'Ville Wallone', CAST(0 AS bit), CAST(50.250000 AS decimal(9,6)), CAST(4.526000 AS decimal(9,6)), 100000, N'Saint-Aubin', NULL, NULL, NULL, CAST(1 AS bit)),
			(58, N'Hanzinelle', N'Ville Wallone', CAST(0 AS bit), CAST(50.290000 AS decimal(9,6)), CAST(4.523000 AS decimal(9,6)), 100000, N'Hanzinelle', NULL, NULL, NULL, CAST(1 AS bit)),
			(59, N'Silenrieux', N'Ville Wallone', CAST(0 AS bit), CAST(50.210000 AS decimal(9,6)), CAST(4.376000 AS decimal(9,6)), 100000, N'Silenrieux', NULL, NULL, NULL, CAST(1 AS bit)),
			(60, N'Marbaix', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.337000 AS decimal(9,6)), 100000, N'Marbaix', NULL, NULL, NULL, CAST(1 AS bit)),
			(61, N'Fromiée', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.556000 AS decimal(9,6)), 100000, N'Fromiée', NULL, NULL, NULL, CAST(1 AS bit)),
			(62, N'Hemptinne', N'Ville Wallone', CAST(0 AS bit), CAST(50.230000 AS decimal(9,6)), CAST(4.520000 AS decimal(9,6)), 100000, N'Hemptinne', NULL, NULL, NULL, CAST(1 AS bit)),
			(63, N'Saint-Gérard', N'Ville Wallone', CAST(0 AS bit), CAST(50.350000 AS decimal(9,6)), CAST(4.638000 AS decimal(9,6)), 100000, N'Saint-Gérard', NULL, NULL, NULL, CAST(1 AS bit)),
			(64, N'Vogenée', N'Ville Wallone', CAST(0 AS bit), CAST(50.240000 AS decimal(9,6)), CAST(4.434000 AS decimal(9,6)), 100000, N'Vogenée', NULL, NULL, NULL, CAST(1 AS bit)),
			(65, N'Bambois', N'Ville Wallone', CAST(0 AS bit), CAST(50.380000 AS decimal(9,6)), CAST(4.682000 AS decimal(9,6)), 100000, N'Bambois', NULL, NULL, NULL, CAST(1 AS bit)),
			(66, N'Franière', N'Ville Wallone', CAST(0 AS bit), CAST(50.430000 AS decimal(9,6)), CAST(4.681000 AS decimal(9,6)), 100000, N'Franière', NULL, NULL, NULL, CAST(1 AS bit)),
			(67, N'Floreffe', N'Ville Wallone', CAST(0 AS bit), CAST(50.432568 AS decimal(9,6)), CAST(4.679970 AS decimal(9,6)), 100000, N'Floreffe', NULL, NULL, NULL, CAST(1 AS bit)),
			(68, N'Jamioulx', N'Ville Wallone', CAST(0 AS bit), CAST(50.350023 AS decimal(9,6)), CAST(4.372106 AS decimal(9,6)), 100000, N'Jamioulx', NULL, NULL, NULL, CAST(1 AS bit)),
			(69, N'Liberchies', N'Ville Wallone', CAST(0 AS bit), CAST(50.520000 AS decimal(9,6)), CAST(4.387000 AS decimal(9,6)), 100000, N'Liberchies', NULL, NULL, NULL, CAST(1 AS bit)),
			(70, N'Sart-Eustache', N'Ville Wallone', CAST(0 AS bit), CAST(50.370000 AS decimal(9,6)), CAST(4.565000 AS decimal(9,6)), 100000, N'Sart-Eustache', NULL, NULL, NULL, CAST(1 AS bit)),
			(71, N'Cerfontaine', N'Ville Wallone', CAST(0 AS bit), CAST(50.180000 AS decimal(9,6)), CAST(4.275000 AS decimal(9,6)), 100000, N'Cerfontaine', NULL, NULL, NULL, CAST(1 AS bit)),
			(72, N'Chastrès', N'Ville Wallone', CAST(0 AS bit), CAST(50.270000 AS decimal(9,6)), CAST(4.423000 AS decimal(9,6)), 100000, N'Chastrès', NULL, NULL, NULL, CAST(1 AS bit)),
			(73, N'Le Roux', N'Ville Wallone', CAST(0 AS bit), CAST(50.390000 AS decimal(9,6)), CAST(4.570000 AS decimal(9,6)), 100000, N'Le Roux', NULL, NULL, NULL, CAST(1 AS bit)),
			(74, N'Mariembourg', N'Ville Wallone', CAST(0 AS bit), CAST(50.100000 AS decimal(9,6)), CAST(4.474000 AS decimal(9,6)), 100000, N'Mariembourg', NULL, NULL, NULL, CAST(1 AS bit)),
			(75, N'Petigny', N'Ville Wallone', CAST(0 AS bit), CAST(50.030000 AS decimal(9,6)), CAST(4.456000 AS decimal(9,6)), 100000, N'Petigny', NULL, NULL, NULL, CAST(1 AS bit)),
			(76, N'Sart-Saint-Laurent', N'Ville Wallone', CAST(0 AS bit), CAST(50.400000 AS decimal(9,6)), CAST(4.674000 AS decimal(9,6)), 100000, N'Srt-St-Laurent', NULL, NULL, NULL, CAST(1 AS bit)),
			(77, N'Beignée', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.377000 AS decimal(9,6)), 100000, N'Beignée', NULL, NULL, NULL, CAST(1 AS bit)),
			(78, N'COUILLET Queue', N'Ville Wallone', CAST(0 AS bit), CAST(50.380000 AS decimal(9,6)), CAST(4.464000 AS decimal(9,6)), 100000, N'COUILLET Queue', NULL, NULL, NULL, CAST(1 AS bit)),
			(79, N'Ham-sur-Heure-Nalinnes', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.329000 AS decimal(9,6)), 100000, N'Ham-s-H-Nalinnes', NULL, NULL, NULL, CAST(1 AS bit)),
			(80, N'Lausprelle', N'Ville Wallone', CAST(0 AS bit), CAST(50.360000 AS decimal(9,6)), CAST(4.488000 AS decimal(9,6)), 100000, N'Lausprelle', NULL, NULL, NULL, CAST(1 AS bit)),
			(81, N'Marcinelle', N'Ville Wallone', CAST(0 AS bit), CAST(50.380000 AS decimal(9,6)), CAST(4.398000 AS decimal(9,6)), 100000, N'Marcinelle', NULL, NULL, NULL, CAST(1 AS bit)),
			(82, N'Pont-de-Loup', N'Ville Wallone', CAST(0 AS bit), CAST(50.410000 AS decimal(9,6)), CAST(4.509000 AS decimal(9,6)), 100000, N'Pont-de-Loup', NULL, NULL, NULL, CAST(1 AS bit)),
			(83, N'Yves-Gomezée', N'Ville Wallone', CAST(0 AS bit), CAST(50.230000 AS decimal(9,6)), CAST(4.424000 AS decimal(9,6)), 100000, N'Yves-Gomezée', NULL, NULL, NULL, CAST(1 AS bit)),
			(84, N'Châtelineau', N'Ville Wallone', CAST(0 AS bit), CAST(50.420000 AS decimal(9,6)), CAST(4.470000 AS decimal(9,6)), 100000, N'Châtelineau', NULL, NULL, NULL, CAST(1 AS bit)),
			(85, N'Soumoy', N'Ville Wallone', CAST(0 AS bit), CAST(50.190000 AS decimal(9,6)), CAST(4.387000 AS decimal(9,6)), 100000, N'Soumoy', NULL, NULL, NULL, CAST(1 AS bit)),
			(86, N'Furnaux', N'Ville Wallone', CAST(0 AS bit), CAST(50.310000 AS decimal(9,6)), CAST(4.660000 AS decimal(9,6)), 100000, N'Furnaux', NULL, NULL, NULL, CAST(1 AS bit)),
			(87, N'GILLY Sart Culpart', N'Ville Wallone', CAST(0 AS bit), CAST(50.430000 AS decimal(9,6)), CAST(4.492000 AS decimal(9,6)), 100000, N'GILLY Srt Culpt', NULL, NULL, NULL, CAST(1 AS bit)),
			(88, N'Loverval', N'Ville Wallone', CAST(0 AS bit), CAST(50.370000 AS decimal(9,6)), CAST(4.440000 AS decimal(9,6)), 100000, N'Loverval', NULL, NULL, NULL, CAST(1 AS bit)),
			(89, N'Roly', N'Ville Wallone', CAST(0 AS bit), CAST(50.130000 AS decimal(9,6)), CAST(4.455000 AS decimal(9,6)), 100000, N'Roly', NULL, NULL, NULL, CAST(1 AS bit)),
			(90, N'Beignée', N'Ville Wallone', CAST(0 AS bit), CAST(50.330000 AS decimal(9,6)), CAST(4.377000 AS decimal(9,6)), 100000, N'Beignée', NULL, NULL, NULL, CAST(1 AS bit)),
			(91, N'Jumet Hamendes', N'Ville Wallone', CAST(0 AS bit), CAST(50.440000 AS decimal(9,6)), CAST(4.453000 AS decimal(9,6)), 100000, N'Jumet Hamds', NULL, NULL, NULL, CAST(1 AS bit)),
			(92, N'Pontaury', N'Ville Wallone', CAST(0 AS bit), CAST(50.340000 AS decimal(9,6)), CAST(4.638000 AS decimal(9,6)), 100000, N'Pontaury', NULL, NULL, NULL, CAST(1 AS bit)),
			(93, N'Presles', N'Ville Wallone', CAST(0 AS bit), CAST(50.390000 AS decimal(9,6)), CAST(4.540000 AS decimal(9,6)), 100000, N'Presles', NULL, NULL, NULL, CAST(1 AS bit)),
			(94, N'Stave', N'Ville Wallone', CAST(0 AS bit), CAST(50.280000 AS decimal(9,6)), CAST(4.575000 AS decimal(9,6)), 100000, N'Stave', NULL, NULL, NULL, CAST(1 AS bit)),
			(95, N'Boubier', N'Ville Wallone', CAST(0 AS bit), CAST(50.400000 AS decimal(9,6)), CAST(4.489000 AS decimal(9,6)), 100000, N'Boubier', NULL, NULL, NULL, CAST(1 AS bit)),
			(96, N'Névremont', N'Ville Wallone', CAST(0 AS bit), CAST(50.410000 AS decimal(9,6)), CAST(4.655000 AS decimal(9,6)), 100000, N'Névremont', NULL, NULL, NULL, CAST(1 AS bit)),
			(97, N'Nismes', N'Ville Wallone', CAST(0 AS bit), CAST(50.050000 AS decimal(9,6)), CAST(4.484000 AS decimal(9,6)), 100000, N'Nismes', NULL, NULL, NULL, CAST(1 AS bit)),
			(98, N'Ragnies', N'Ville Wallone', CAST(0 AS bit), CAST(50.320000 AS decimal(9,6)), CAST(4.188000 AS decimal(9,6)), 100000, N'Ragnies', NULL, NULL, NULL, CAST(1 AS bit)),
			(99, N'Wangenies', N'Ville Wallone', CAST(0 AS bit), CAST(50.480000 AS decimal(9,6)), CAST(4.496000 AS decimal(9,6)), 100000, N'Wangenies', NULL, NULL, NULL, CAST(1 AS bit)),
			(100, N'Fosses-la-Ville', N'Ville Wallone', CAST(0 AS bit), CAST(50.390000 AS decimal(9,6)), CAST(4.527000 AS decimal(9,6)), 100000, N'Fosses-l-V', NULL, NULL, NULL, CAST(1 AS bit)),
			(101, N'Senzeille', N'Ville Wallone', CAST(0 AS bit), CAST(50.160000 AS decimal(9,6)), CAST(4.377000 AS decimal(9,6)), 100000, N'Senzeille', NULL, NULL, NULL, CAST(1 AS bit)),
			(102, N'Flavion', N'Ville Wallone', CAST(0 AS bit), CAST(50.260000 AS decimal(9,6)), CAST(4.688000 AS decimal(9,6)), 100000, N'Flavion', NULL, NULL, NULL, CAST(1 AS bit)),
			(103, N'Aisemont', N'Ville Wallone', CAST(0 AS bit), CAST(50.410000 AS decimal(9,6)), CAST(4.609000 AS decimal(9,6)), 100000, N'Aisemont', NULL, NULL, NULL, CAST(1 AS bit)),
			(104, N'Devant-les-Bois', N'Ville Wallone', CAST(0 AS bit), CAST(50.350000 AS decimal(9,6)), CAST(4.619000 AS decimal(9,6)), 100000, N'Dvt-les-Bois', NULL, NULL, NULL, CAST(1 AS bit)),
			(105, N'Froidchapelle', N'Ville Wallone', CAST(0 AS bit), CAST(50.160000 AS decimal(9,6)), CAST(4.177000 AS decimal(9,6)), 100000, N'Froidchapelle', NULL, NULL, NULL, CAST(1 AS bit)),
			(106, N'Bastogne', N'Ville Wallone', CAST(0 AS bit), CAST(50.010000 AS decimal(9,6)), CAST(5.374000 AS decimal(9,6)), 100000, N'Bastogne', NULL, NULL, NULL, CAST(1 AS bit)),
			(107, N'Haut-Vent', N'Ville Wallone', CAST(0 AS bit), CAST(50.390000 AS decimal(9,6)), CAST(4.665000 AS decimal(9,6)), 100000, N'Haut-Vent', NULL, NULL, NULL, CAST(1 AS bit)),
			(108, N'Forchies-la-Marche', N'Ville Wallone', CAST(0 AS bit), CAST(50.430000 AS decimal(9,6)), CAST(4.278000 AS decimal(9,6)), 100000, N'Forchies-l-M', NULL, NULL, NULL, CAST(1 AS bit)),
			(109, N'Sombreffe', N'Ville Wallone', CAST(0 AS bit), CAST(50.530000 AS decimal(9,6)), CAST(4.596000 AS decimal(9,6)), 100000, N'Sombreffe', NULL, NULL, NULL, CAST(1 AS bit))
			
        ) AS V
        (
            Id,
            Name,
            Type,
            Indoor,
            Latitude,
            Longitude,
            Capacity,
            Tag,
            ExternalSource,
            ExternalId,
            SourceUpdatedAtUtc,
            Active
        )
    )
    MERGE dbo.Place AS T 
    USING SourcePlace AS S
        ON T.Id = S.Id
    WHEN MATCHED THEN
        UPDATE SET
            T.Name = S.Name,
            T.Type = S.Type,
            T.Indoor = S.Indoor,
            T.Latitude = S.Latitude,
            T.Longitude = S.Longitude,
            T.Capacity = S.Capacity,
            T.Tag = S.Tag,
            T.ExternalSource = S.ExternalSource,
            T.ExternalId = S.ExternalId,
            T.SourceUpdatedAtUtc = S.SourceUpdatedAtUtc,
            T.Active = S.Active
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            Id,
            Name,
            Type,
            Indoor,
            Latitude,
            Longitude,
            Capacity,
            Tag,
            ExternalSource,
            ExternalId,
            SourceUpdatedAtUtc,
            Active
        )
        VALUES
        (
            S.Id,
            S.Name,
            S.Type,
            S.Indoor,
            S.Latitude,
            S.Longitude,
            S.Capacity,
            S.Tag,
            S.ExternalSource,
            S.ExternalId,
            S.SourceUpdatedAtUtc,
            S.Active
        );

    PRINT 'POSTDEPLOY SEED: Place MERGE done';

    SET IDENTITY_INSERT dbo.Place OFF;


    SET IDENTITY_INSERT dbo.CrowdCalendar ON;

    ;WITH SourceCrowdCalendar AS
    (
        SELECT *
        FROM (VALUES
            (1, CONVERT(date, '2026-04-30', 23), N'Bouffioulx', 2, N'Marche St Blaise et St Etienne', 4, 100, CAST(50.383927 AS decimal(9,6)), CAST(4.46969 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Bouffioulx en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Blaise & St Etienne', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 14:07:33.000', 121)),
			(2, CONVERT(date, '2026-05-01', 23), N'Vedrin', 3, N'Marche St Eloi', 4, 100, CAST(50.494581 AS decimal(9,6)), CAST(4.830746 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Vedrin en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Eloi', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 14:19:31.000', 121)),
			(3, CONVERT(date, '2026-05-02', 23), N'Bouffioulx', 2, N'Marche St Blaise et St Etienne', 4, 100, CAST(50.383927 AS decimal(9,6)), CAST(4.469693 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Bouffioulx en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Blaise & St Etienne', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 14:23:02.000', 121)),
			(4, CONVERT(date, '2026-05-03', 23), N'Aiseau',4, N'Marche Royale St Martin', 4, 100, CAST(50.418635 AS decimal(9,6)), CAST(4.552935 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Aiseau en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M R St Martin', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 14:33:33.000', 121)),
			(5, CONVERT(date, '2026-05-03', 23), N'Feluy', 5, N'Marche Ste Aldegonde', 4, 100, CAST(50.562611 AS decimal(9,6)), CAST(4.157569 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Feluy en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Aldegonde', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 14:55:35.000', 121)),
			(6, CONVERT(date, '2026-05-03', 23), N'Philippeville', 6, N'Marche St Philippe', 4, 100, CAST(50.161225 AS decimal(9,6)), CAST(4.437995 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Philippeville en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Philippe', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 15:01:37.000', 121)),
			(7, CONVERT(date, '2026-05-03', 23), N'Tarcienne', 7, N'Marche St Fiacre', 4, 100, CAST(50.310983 AS decimal(9,6)), CAST(4.449996 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Tarcienne en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Fiacre', CAST(0 AS bit), CONVERT(datetime2, '2026-05-08 15:07:14.000', 121)),
			(8, CONVERT(date, '2026-05-10', 23), N'Dourbes', 8, N'Marche St Servais', 4, 100, CAST(50.257568 AS decimal(9,6)), CAST(4.379819 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Dourbes en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Servais', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:15:29.000', 121)),
			(9, CONVERT(date, '2026-05-10', 23), N'Pry', 9, N'Marche St Remfroid', 4, 100, CAST(50.257568 AS decimal(9,6)), CAST(4.379819 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Pry en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Remfroid', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:18:45.000', 121)),
			(10, CONVERT(date, '2026-05-17', 23), N'Aiseau', 4, N'Marche Royale St Martin', 4, 100, CAST(50.418635 AS decimal(9,6)), CAST(4.552935 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Aiseau en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Royale St Martin', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:28:42.000', 121)),
			(11, CONVERT(date, '2026-05-17', 23), N'Bouffioulx', 2, N'Marche St Géry', 4, 100, CAST(50.383927 AS decimal(9,6)), CAST(4.469693 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Bouffioulx en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Géry', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:32:34.000', 121)),
			(12, CONVERT(date, '2026-05-17', 23), N'Châtelet', 10, N'Marche St Roch', 4, 100, CAST(50.405939 AS decimal(9,6)), CAST(4.440547 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Châtelet en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:40:28.000', 121)),
			(13, CONVERT(date, '2026-05-17', 23), N'Ragnies', 11, N'Marche St Roch', 4, 100, CAST(50.317325 AS decimal(9,6)), CAST(4.188391 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Ragnies en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:44:23.000', 121)),
			(14, CONVERT(date, '2026-05-17', 23), N'Thuin', 12, N'Marche St Roch', 4, 100, CAST(50.322062 AS decimal(9,6)), CAST(4.145776 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Thuin en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:48:43.000', 121)),
			(15, CONVERT(date, '2026-05-24', 23), N'Hanzinne', 13, N'Marche St Oger', 4, 100, CAST(50.309200 AS decimal(9,6)), CAST(4.513569 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Hanzinne en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Oger', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:53:56.000', 121)),
			(16, CONVERT(date, '2026-05-24', 23), N'Malonne', 14, N'Marche St Berthuin', 4, 100, CAST(50.434937 AS decimal(9,6)), CAST(4.766989 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Malonne en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Berthuin', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:58:33.000', 121)),
			(17, CONVERT(date, '2026-05-25', 23), N'Acoz', 15, N'Marche Ste Rolende', 4, 100, CAST(50.356715 AS decimal(9,6)), CAST(4.477717 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Acoz en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit),	CONVERT(datetime2, '2026-05-08 16:03:48.000', 121)),
			(18, CONVERT(date, '2026-05-25', 23), N'Biesme', 16, N'Marche Ste Rolende', 4, 100, CAST(50.328303 AS decimal(9,6)), CAST(4.528808 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Biesme en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:07:24.000', 121)),
			(19, CONVERT(date, '2026-05-25', 23), N'Fromiée', 17, N'Marche Ste Rolende', 4, 100, CAST(50.333069 AS decimal(9,6)), CAST(4.555583  AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Fromiée en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:00:57.000', 121)),
			(20, CONVERT(date, '2026-05-25', 23), N'Gerpinnes', 18, N'Marche Ste Rolende', 4, 100, CAST(50.347185 AS decimal(9,6)), CAST(4.443540 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Gerpinnes en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:05:35.000', 121)),
			(21, CONVERT(date, '2026-05-25', 23), N'Gougnies', 19, N'Marche Ste Rolende', 4, 100, CAST(50.358146 AS decimal(9,6)), CAST(4.557336 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Gougnies en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:10:24.000', 121)),
			(22, CONVERT(date, '2026-05-25', 23), N'Hymiée', 20, N'Marche Ste Rolende', 4, 100, CAST(50.325105 AS decimal(9,6)), CAST(4.518760 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Hymiée en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:47:15.000', 121)),
			(23, CONVERT(date, '2026-05-25', 23), N'Joncret',	21, N'Marche Ste Rolende', 4, 100, CAST(50.339554 AS decimal(9,6)), CAST(4.471094 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Joncret en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:52:37.000', 121)),
			(24, CONVERT(date, '2026-05-25', 23), N'Les Flaches',	22, N'Marche Ste Rolende', 4, 100, CAST(50.339554 AS decimal(9,6)), CAST(4.471094 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Les Flaches en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:56:29.000', 121)),
			(25, CONVERT(date, '2026-05-25', 23), N'Tarcienne', 23, N'Marche Ste Rolende', 4, 100, CAST(50.310983 AS decimal(9,6)), CAST(4.449996 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Tarcienne en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:00:32.000', 121)),
			(26, CONVERT(date, '2026-05-25', 23), N'Villers-Poterie', 24, N'Marche Ste Rolendee', 4, 100, CAST(50.356266 AS decimal(9,6)), CAST(4.531074 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Villers-Poterie en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M Ste Rolende', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:04:14.000', 121)),
			(27, CONVERT(date, '2026-05-30', 23), N'Ligny', 25, N'Reconstitution 1815', 4, 100, CAST(50.508861 AS decimal(9,6)), CAST(4.534085 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Ligny en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'Reconst 1815', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:10:27.000', 121)),
			(28, CONVERT(date, '2026-05-25', 23), N'Farciennes', 26, N'Marche St Joseph et St Rémy', 4, 100, CAST(50.432463 AS decimal(9,6)), CAST(4.507769 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Farciennes en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Joseph & St Rémy', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:17:02.000', 121)),
			(29, CONVERT(date, '2026-05-31', 23), N'Daussois', 27	, N'Marche St Vaast', 4, 100, CAST(50.213742 AS decimal(9,6)), CAST(4.416490 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Daussois en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Vaast', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:22:22.000', 121)),
			(30, CONVERT(date, '2026-05-31', 23), N'Mons', 28, N'El Doudou', 4, 100, CAST(50.445865 AS decimal(9,6)), CAST(3.805023 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Mons en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'El Doudou', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:27:32.000', 121)),
			(31, CONVERT(date, '2026-05-31', 23), N'Walcourt', 29, N'Marche Notre Dame de Walcourt', 4, 100, CAST(50.272617 AS decimal(9,6)), CAST(4.250725 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Walcourt en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M ND de Walcourt', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:34:37.000', 121)),
			(32, CONVERT(date, '2026-06-07', 23), N'Biesme', 16, N'Marche du Saint Sacrement', 4, 100, CAST(50.383927 AS decimal(9,6)), CAST(4.469693 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Biesme en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M du St Sacrement', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:58:11.000', 121)),
			(33, CONVERT(date, '2026-06-07', 23), N'Boussu-lez-Walcourt',30, N'Marche du Saint Sacrement', 4, 100, CAST(50.207293 AS decimal(9,6)), CAST(4.287454 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Boussu-lez-Walcourt en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M du St Sacrement', CAST(1 AS bit), CONVERT(datetime2(3), '2026-05-08 19:03:29.000', 121)),
			(34, CONVERT(date, '2026-06-07', 23), N'COUILLET Centre', 31, N'Marche St Basile', 4, 100, CAST(50.392543 AS decimal(9,6)), CAST(4.465698 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez COUILLET Centre en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Basile', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:09:26.000', 121)),
			(35, CONVERT(date, '2026-06-07', 23), N'Walcourt', 29, N'Marche Notre Dame de Walcourt', 4, 100, CAST(50.392543 AS decimal(9,6)), CAST(4.465698 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Walcourt en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M ND de Walcourt', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:12:42.000', 121)),
			(36, CONVERT(date, '2026-06-14', 23), N'Maison-St-Gérard', 32, N'Marche St Nicolas', 4, 100, CAST(50.365875 AS decimal(9,6)), CAST(4.692264 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Maison-St-Gérard en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Nicolas', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:19:25.000', 121)),
			(37, CONVERT(date, '2026-06-14', 23), N'Solre-sur-Sambre', 33, N'Marche St Medard', 4, 100, CAST(50.301834 AS decimal(9,6)), CAST(4.107803 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Solre-sur-Sambre en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Medard', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:25:39.000', 121)),
			(38, CONVERT(date, '2026-06-21', 23), N'Cour-sur-Heure', 34, N'Marche St Jean-Baptiste', 4, 100, CAST(50.302540 AS decimal(9,6)), CAST(4.348912 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Cour-sur-Heure en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St J-Baptiste', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:36:57.000', 121)),
			(39, CONVERT(date, '2026-06-21', 23), N'Gourdinne', 35, N'Marche St Walère', 4, 100, CAST(50.294671 AS decimal(9,6)), CAST(4.432649 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Gourdinne en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Walère', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:42:14.000', 121)),
			(40, CONVERT(date, '2026-06-27', 23), N'Waterloo', 36, N'Battle reconstitution 1815', 4, 100, CAST(50.678475 AS decimal(9,6)), CAST(4.402263 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 48, N'Evitez Waterloo Battle Field en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'B r 1815', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:50:41.000', 121)),
			(41, CONVERT(date, '2026-06-28', 23), N'Laneffe', 37, N'Marche St Eloi', 4, 100, CAST(50.275977 AS decimal(9,6)), CAST(4.455329 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Laneffe en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Eloi', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 19:57:35.000', 121)),
			(42, CONVERT(date, '2026-06-28', 23), N'Mettet', 38, N'Marche St Jean', 4, 100, CAST(50.320744 AS decimal(9,6)), CAST(4.510084 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Mettet en empruntant les itinéraires de contournements prévus ou assistez aux événements', N'M St Jean', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 20:02:47.000', 121)),
			(43, CONVERT(date, '2026-07-05', 23), N'Biesmerée', 39, N'Marche St Pierre', 4, 100, CAST(50.290855 AS decimal(9,6)), CAST(4.632562 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Biesmerée en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 10:50:48.000', 121)),
			(44, CONVERT(date, '2026-07-05', 23), N'Florennes', 40, N'Marche St Pierre & Paul', 4, 100, CAST(50.260368 AS decimal(9,6)), CAST(4.481467 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Florennes en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre & Paul', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 11:12:47.000', 121)),
			(45, CONVERT(date, '2026-07-05', 23), N'Monceau-sur-Sambre', 41, N'Marche St Louis de Gonzague', 4, 100, CAST(50.413313 AS decimal(9,6)), CAST(4.333497 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Monceau-sur-Sambre en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Louis de G', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 11:16:42.000', 121)),
			(46, CONVERT(date, '2026-07-05', 23), N'Morialmé', 42, N'Marche St Pierre', 4, 100, CAST(50.277785 AS decimal(9,6)), CAST(4.489152 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Morialmé en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:09:43.000', 121)),
			(47, CONVERT(date, '2026-07-05', 23), N'Thy-le-Château', 43, N'Marche St Pierre & Paul', 4, 100, CAST(50.291485 AS decimal(9,6)), CAST(4.390856 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Thy-le-Château en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre & Paul', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:13:03.000', 121)),
			(48, CONVERT(date, '2026-07-05', 23), N'Villers-Deux-Églises', 45, N'Marche St Pierre', 4, 100, CAST(50.192680 AS decimal(9,6)), CAST(4.445388 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Villers-Deux-Églises en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:17:50.000', 121)),
			(49, CONVERT(date, '2026-07-05', 23), N'Vitrival', 46, N'Marche St Pierre', 4, 100, CAST(50.384903 AS decimal(9,6)), CAST(4.606561 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Vitrival en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:20:45.000', 121)),
			(50, CONVERT(date, '2026-07-12', 23), N'Chamborgneau', 47, N'Marche Notre Dame de Lourdes', 4, 100, CAST(50.377497 AS decimal(9,6)), CAST(4.487094 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Chamborgneau en empruntant les itinéraires prévus ou assistez aux célébrations', N'M ND de Lourdes', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:35:54.000', 121)),
			(51, CONVERT(date, '2026-07-12', 23), N'Nalinnes', 48, N'Marche Notre Dame de Bon Secours', 4, 100, CAST(50.333450 AS decimal(9,6)), CAST(4.401953 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Nalinnes en empruntant les itinéraires prévus ou assistez aux célébrations', N'M ND de Bon Secours', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:40:01.000', 121)),
			(52, CONVERT(date, '2026-07-12', 23), N'Somzée', 49, N'Marche Notre Dame de Beauraing', 4, 100, CAST(50.294116 AS decimal(9,6)), CAST(4.456868 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Somzée en empruntant les itinéraires prévus ou assistez aux célébrations', N'M ND de Beauraing', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:43:51.000', 121)),
			(53, CONVERT(date, '2026-07-12', 23), N'Thy-le-Bauduin', 50, N'Marche St Pierre', 4, 100, CAST(50.296215 AS decimal(9,6)), CAST(4.502360 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Thy-le-Bauduin en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:47:42.000', 121)),
			(54, CONVERT(date, '2026-07-12', 23), N'Virelles', 51, N'Marche Notre Dame de Lumière', 4, 100, CAST(50.082372 AS decimal(9,6)), CAST(4.252215 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Virelles en empruntant les itinéraires prévus ou assistez aux célébrations', N'M ND de Lumière', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:51:58.000', 121)),
			(55, CONVERT(date, '2026-07-19', 23), N'Berzée', 52, N'Marche Ste Maguerite', 4, 100, CAST(50.293841 AS decimal(9,6)), CAST(4.351321 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Berzée en empruntant les itinéraires prévus ou assistez aux célébrations', N'M Ste Maguerite', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 12:55:47.000', 121)),
			(56, CONVERT(date, '2026-07-19', 23), N'Fraire', 53, N'Marche St Ghislain', 4, 100, CAST(50.258284 AS decimal(9,6)), CAST(4.460615 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Fraire en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Ghislain', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:00:26.000', 121)),
			(57, CONVERT(date, '2026-07-19', 23), N'Jumet', 54, N'Marche Tour de Marie-Madelaine', 4, 100, CAST(50.444402 AS decimal(9,6)), CAST(4.360657 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Jumet en empruntant les itinéraires prévus ou assistez aux célébrations', N'M T la Madelaine', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:05:31.000', 121)),
			(58, CONVERT(date, '2026-07-19', 23), N'Oret', 55, N'Marche St Remfroid', 4, 100, CAST(50.300091 AS decimal(9,6)), CAST(4.589564 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Oret en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Remfroid', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:10:57.000', 121)),
			(59, CONVERT(date, '2026-07-19', 23), N'Rosée', 56, N'Marche St Remy', 4, 100, CAST(50.226785 AS decimal(9,6)), CAST(4.601228 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Rosée en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Remy', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:16:43.000', 121)),
			(60, CONVERT(date, '2026-07-19', 23), N'Saint-Aubin', 57, N'Marche Notre Dame du Mont Carmel', 4, 100, CAST(50.247186 AS decimal(9,6)), CAST(4.526094 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Saint-Aubin en empruntant les itinéraires prévus ou assistez aux célébrations', N'M ND du Mt Carmel', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:21:12.000', 121)),
			(61, CONVERT(date, '2026-07-19', 23), N'Hanzinelle', 57, N'Marche St Christophe', 4, 100, CAST(50.247186 AS decimal(9,6)), CAST(4.526094 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Hanzinelle en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Christophe', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:24:54.000', 121)),
			(62, CONVERT(date, '2026-07-26', 23), N'Silenrieux', 59, N'Marche Ste Anne', 4, 100, CAST(50.212411 AS decimal(9,6)), CAST(4.375523 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Silenrieux en empruntant les itinéraires prévus ou assistez aux célébrations', N'M Ste Anne', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:30:46.000', 121)),
			(63, CONVERT(date, '2026-07-26', 23), N'Hanzinelle', 58, N'Marche St Christophe', 4, 100, CAST(50.293803 AS decimal(9,6)), CAST(4.523451 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Hanzinelle en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Christophe', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:34:15.000', 121)),
			(64, CONVERT(date, '2026-07-26', 23), N'Marbaix', 60, N'Marche St Christophe', 4, 100, CAST(50.333688 AS decimal(9,6)), CAST(4.336753 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Marbaix en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Christophe', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:37:38.000', 121)),
			(65, CONVERT(date, '2026-07-26', 23), N'Fromiée', 61, N'Marche Ste Adèle', 4, 100, CAST(50.333069 AS decimal(9,6)), CAST(4.555583 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Fromiée en empruntant les itinéraires prévus ou assistez aux célébrations', N'M Ste Adèle', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:41:34.000', 121)),
			(66, CONVERT(date, '2026-07-26', 23), N'Hemptinne', 62, N'Marche St Walhère', 4, 100, CAST(50.226806 AS decimal(9,6)), CAST(4.519996 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Hemptinne en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Walhère', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:45:02.000', 121)),
			(67, CONVERT(date, '2026-07-26', 23), N'Saint-Gérard', 63, N'Marche St Gérard de Brogne', 4, 100, CAST(50.353435 AS decimal(9,6)), CAST(4.637927 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Saint-Gérard en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St Gé de Brogne', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:48:54.000', 121)),
			(68, CONVERT(date, '2026-07-26', 23), N'Vogenée', 64, N'Marche St André', 4, 100, CAST(50.237748 AS decimal(9,6)), CAST(4.434490 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Vogenée en empruntant les itinéraires prévus ou assistez aux célébrations', N'M St André', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 13:53:18.000', 121)),
			(69, CONVERT(date, '2026-07-29', 23), N'Bambois', 65, N'My self B-Day', 4, 100, CAST(50.375841 AS decimal(9,6)), CAST(4.682315 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '23:59:59', 108), 14, N'Allez vous faire foutre. C''est mon anniversaire aujourd''hui', N'My self B-Day', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:03:37.000', 121)),
			(70, CONVERT(date, '2026-08-09', 23), N'COUILLET Centre', 31, N'Marche St Laurent', 4, 100, CAST(50.392543 AS decimal(9,6)), CAST(4.465698 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez COUILLET Centre en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Laurent', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:09:10.000', 121)),
			(71, CONVERT(date, '2026-07-05', 23), N'Franière',66, N'Marche Saints-Pierre-et-Paul', 4, 100, CAST(50.428741 AS decimal(9,6)), CAST(4.680875 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Franière en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Pierre & Paul', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:15:10.000', 121)),
			(72, CONVERT(date, '2026-08-09', 23), N'Floreffe', 67, N'Marche St Roch', 4, 100, CAST(50.432568 AS decimal(9,6)), CAST(4.679970 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Floreffe en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:19:30.000', 121)),
			(73, CONVERT(date, '2026-08-09', 23), N'Jamioulx', 68, N'Marche St André', 4, 100, CAST(50.350023 AS decimal(9,6)), CAST(4.372106 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Jamioulx en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St André', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:24:07.000', 121)),
			(74, CONVERT(date, '2026-08-09', 23), N'Liberchies', 69, N'Marche St Pierre', 4, 100, CAST(50.515443 AS decimal(9,6)), CAST(4.386796 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Liberchies en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:28:17.000', 121)),
			(75, CONVERT(date, '2026-08-09', 23), N'Sart-Eustache', 70, N'Marche St Roch', 4, 100, CAST(50.370651 AS decimal(9,6)), CAST(4.565064 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Sart-Eustache en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:32:19.000', 121)),
			(76, CONVERT(date, '2026-08-09', 23), N'Villers-Poterie', 24, N'Marche St Martin', 4, 100, CAST(50.356266 AS decimal(9,6)), CAST(4.531074 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Villers-Poterie en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Martin', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:35:07.000', 121)),
			(77, CONVERT(date, '2026-08-15', 23), N'Cerfontaine', 71, N'Marche St Laurent', 4, 100, CAST(50.181321 AS decimal(9,6)), CAST(4.275415 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Cerfontaine en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Laurent', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:39:35.000', 121)),
			(78, CONVERT(date, '2026-08-15', 23), N'Chastrès', 72, N'Marche St Roch', 4, 100, CAST(50.267907 AS decimal(9,6)), CAST(4.422947 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Chastrès en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:44:32.000', 121)),
			(79, CONVERT(date, '2026-08-15', 23), N'Le Roux', 73, N'Marche Ste Gertrude', 4, 100, CAST(50.385810 AS decimal(9,6)), CAST(4.570367 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Le Roux en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M Ste Gertrude', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:48:18.000', 121)),
			(80, CONVERT(date, '2026-08-15', 23), N'Mariembourg', 74, N'Marche Notre Dame de la Brouffe', 4, 100, CAST(50.104458 AS decimal(9,6)), CAST(4.474143 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Mariembourg en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M ND de la Brouffe', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:52:30.000', 121)),
			(81, CONVERT(date, '2026-08-15', 23), N'Petigny', 75, N'Marche St Victor', 4, 100, CAST(50.029909 AS decimal(9,6)), CAST(4.456223 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Petigny en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Victor', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 14:57:04.000', 121)),
			(82, CONVERT(date, '2026-08-15', 23), N'Sart-Saint-Laurent', 76, N'Marche St Laurent', 4, 100, CAST(50.399046 AS decimal(9,6)), CAST(4.673925 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Sart-Saint-Laurent en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Laurent', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:01:30.000', 121)),
			(83, CONVERT(date, '2026-08-16', 23), N'Acoz', 15, N'Marche St Roch & St Frégo', 4, 100, CAST(50.356715 AS decimal(9,6)), CAST(4.477717 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Acoz en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch & St Frégo', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:05:23.000', 121)),
			(84, CONVERT(date, '2026-08-16', 23), N'Beignée', 71, N'Marche St Roch', 4, 100, CAST(50.332512 AS decimal(9,6)), CAST(4.377365 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Beignée en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:08:52.000', 121)),
			(85, CONVERT(date, '2026-08-16', 23), N'COUILLET Queue', 78, N'Marche St Roch', 4, 100, CAST(50.378218 AS decimal(9,6)), CAST(4.463692 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez COUILLET Queue en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:13:23.000', 121)),
			(86, CONVERT(date, '2026-08-16', 23), N'Ham-sur-Heure-Nalinnes', 79, N'Marche St Roch', 4, 100, CAST(50.325839 AS decimal(9,6)), CAST(4.329110 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Ham-sur-Heure-Nalinnes en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:17:23.000', 121)),
			(87, CONVERT(date, '2026-08-16', 23), N'Lausprelle', 80, N'Marche St Roch & St Frégo', 4, 100, CAST(50.364977 AS decimal(9,6)), CAST(4.488419 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Lausprelle en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Roch & St Frégo', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:21:24.000', 121)),
			(88, CONVERT(date, '2026-08-23', 23), N'Bambois', 65, N'Marche St Barthélemy', 4, 100, CAST(50.375841 AS decimal(9,6)), CAST(4.682315 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Bambois en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Barthélemy', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:44:49.000', 121)),
			(89, CONVERT(date, '2026-08-23', 23), N'Marcinelle', 81, N'Marche St Louis', 4, 100, CAST(50.382240 AS decimal(9,6)), CAST(4.398331 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Marcinelle en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Louis', CAST(1 AS bit), CONVERT(datetime2, '2026-04-30 15:48:45.000', 121)),
			(90, CONVERT(date, '2026-08-23', 23), N'Pont-de-Loup', 82, N'Marche Notre Dame del Manock', 4, 100, CAST(50.411098 AS decimal(9,6)), CAST(4.508707 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Pont-de-Loup en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M ND del Manock', CAST(1 AS bit), CONVERT(datetime2(3), '2026-05-08 15:52:29.000', 121)),
			(91, CONVERT(date, '2026-08-23', 23), N'Yves-Gomezée',83, N'Marche St Laurent', 4, 100, CAST(50.234975 AS decimal(9,6)), CAST(4.424150 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Yves-Gomezée en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Laurent', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 15:55:30.000', 121)),
			(92, CONVERT(date, '2026-08-30', 23), N'Biesme', 16, N'Marche St Martin', 4, 100, CAST(50.328303 AS decimal(9,6)), CAST(4.528808 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Biesme en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Martin', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:26:48.000', 121)),
			(93, CONVERT(date, '2026-08-30', 23), N'Châtelineau', 84, N'Marche Notre Dame de Rome', 4, 100, CAST(50.422490 AS decimal(9,6)), CAST(4.470272 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Châtelineau en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M ND de Rome', CAST(1 AS bit),	CONVERT(datetime2, '2026-05-08 16:30:27.000', 121)),
			(94, CONVERT(date, '2026-08-30', 23), N'Les Flaches', 22, N'Marche St Ghislain', 4, 100, CAST(50.339554 AS decimal(9,6)), CAST(4.471094 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Les Flaches en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Ghislain', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:33:50.000', 121)),
			(95, CONVERT(date, '2026-08-30', 23), N'Soumoy', 85, N'Marche St André', 4, 100, CAST(50.189386 AS decimal(9,6)), CAST(4.387316 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Soumoy en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St André', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:37:21.000', 121)),
			(96, CONVERT(date, '2026-09-06', 23), N'Furnaux', 86, N'Marche Notre Dame de la Nativité', 4, 100, CAST(50.308537 AS decimal(9,6)), CAST(4.660253 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Furnaux en empruntant les itinéraires prévus ou assistez aux célébrations', N'M ND de la Nativité', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:41:37.000', 121)),
			(97, CONVERT(date, '2026-09-06', 23), N'GILLY Sart Culpart', 87, N'Marche St Pierre', 4, 100, CAST(50.428718 AS decimal(9,6)), CAST(4.492047 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez GILLY Sart Culpart en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:45:41.000', 121)),
			(98, CONVERT(date, '2026-09-06', 23), N'Loverval', 88, N'Marche St Hubert', 4, 100, CAST(50.308537 AS decimal(9,6)), CAST(4.660253 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Loverval en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Hubert', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:49:58.000', 121)),
			(99, CONVERT(date, '2026-09-06', 23), N'Roly', 89, N'Marche St Denis', 4, 100, CAST(50.132063 AS decimal(9,6)), CAST(4.454869 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Roly en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Denis', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:53:28.000', 121)),
			(100, CONVERT(date, '2026-09-13', 23), N'Beignée', 90, N'Marche Bienheureux Richard', 4, 100, CAST(50.332512 AS decimal(9,6)), CAST(4.377365 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Beignée en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M Bh Richard', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 16:57:31.000', 121)),
			(101, CONVERT(date, '2026-09-13', 23), N'Gerpinnes', 18, N'Marche Saint Pierre', 4, 100, CAST(50.347185 AS decimal(9,6)), CAST(4.443540 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Gerpinnes en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Pierre', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:00:33.000', 121)),
			(102, CONVERT(date, '2026-09-13', 23), N'Jumet Hamendes',91, N'Marche Sainte Rita', 4, 100, CAST(50.440599 AS decimal(9,6)), CAST(4.453136 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Jumet Hamendes en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M Ste Rita', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:04:31.000', 121)),
			(103, CONVERT(date, '2026-09-13', 23), N'Pontaury', 92, N'Marche Royale St Antoine', 4, 100, CAST(50.338885 AS decimal(9,6)), CAST(4.637700 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Pontaury en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M R St Antoine', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:08:19.000', 121)),
			(104, CONVERT(date, '2026-09-13', 23), N'Presles', 93, N'Marche St Remy', 4, 100, CAST(50.386159 AS decimal(9,6)), CAST(4.540378 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Presles en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Remy', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:12:04.000', 121)),
			(105, CONVERT(date, '2026-09-13', 23), N'Stave', 94, N'Marche St Gérard', 4, 100, CAST(50.278447 AS decimal(9,6)), CAST(4.574899 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Stave en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Gérard', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:19:10.000', 121)),
			(106, CONVERT(date, '2026-09-20', 23), N'Boubier', 95, N'Marche Notre Dame de la Patience', 4, 100, CAST(50.401590 AS decimal(9,6)), CAST(4.489440 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Boubier en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M ND d l Patience', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:23:12.000', 121)),
			(107, CONVERT(date, '2026-09-20', 23), N'Névremont', 96, N'Marche Royal St Rémy', 4, 100, CAST(50.407050 AS decimal(9,6)), CAST(4.655346 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Névremont en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M R St Rémy', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:27:05.000', 121)),
			(108, CONVERT(date, '2026-09-20', 23), N'Nismes', 97, N'Marche St Lambert', 4, 100, CAST(50.051321 AS decimal(9,6)), CAST(4.483798 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Nismes en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Lambert', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:30:20.000', 121)),
			(109, CONVERT(date, '2026-09-20', 23), N'Ragnies', 98, N'Marche St Véron', 4, 100, CAST(50.317325 AS decimal(9,6)), CAST(4.188391 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Ragnies en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Véron', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:33:30.000', 121)),
			(110, CONVERT(date, '2026-09-20', 23), N'Wangenies', 99, N'Marche St Lambert', 4, 100, CAST(50.477436 AS decimal(9,6)), CAST(4.495855 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Wangenies en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Lambert', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:37:52.000', 121)),
			(111, CONVERT(date, '2026-09-27', 23), N'Fosses-la-Ville', 100, N'MARCHE SAINT FEUILLEN', 4, 100, CAST(50.393326 AS decimal(9,6)), CAST(4.526654 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Fosses-la-Ville en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M ST FEUILLEN', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:41:56.000', 121)),
			(112, CONVERT(date, '2026-09-27', 23), N'Senzeille', 101, N'Marche St Martin', 4, 100, CAST(50.162400 AS decimal(9,6)), CAST(4.377051 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Senzeille en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Martin', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:45:19.000', 121)),
			(113, CONVERT(date, '2026-09-27', 23), N'Flavion', 102, N'Marche St Martin', 4, 100, CAST(50.257189 AS decimal(9,6)), CAST(4.687794 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Flavion en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Martin', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:47:59.000', 121)),
			(114, CONVERT(date, '2026-10-04', 23), N'Aisemont', 103, N'Marche Royal Notre Dame', 4, 100, CAST(50.411393 AS decimal(9,6)), CAST(4.608827 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Aisemont en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M R Notre Dame', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:52:01.000', 121)),
			(115, CONVERT(date, '2026-10-04', 23), N'Aisemont fosses aux chênes', 103, N'Marche Royal Notre Dame', 4, 100, CAST(50.420039 AS decimal(9,6)), CAST(4.657386 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '12:30:00', 108), 4, N'Evitez Aisemont fosses aux chênes en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M R Notre Dame', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:54:51.000', 121)),
			(116, CONVERT(date, '2026-10-05', 23), N'Aisemont rue du Fays', 103, N'Marche Royal Notre Dame', 4, 100, CAST(50.405472 AS decimal(9,6)), CAST(4.660349 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Aisemont rue du Fays en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M R Notre Dame', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 17:57:13.000', 121)),
			(117, CONVERT(date, '2026-10-11', 23), N'Devant-les-Bois', 104, N'Marche St Joseph', 4, 100, CAST(50.346821 AS decimal(9,6)), CAST(4.618795 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Devant-les-Bois en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Joseph', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:01:11.000', 121)),
			(118, CONVERT(date, '2026-10-11', 23), N'Froidchapelle', 105, N'Marche Ste Aldegonde', 4, 100, CAST(50.163228 AS decimal(9,6)), CAST(4.176753 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Froidchapelle en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M Ste Aldegonde', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:05:30.000', 121)),
			(119, CONVERT(date, '2026-10-11', 23), N'Malonne', 14, N'Marche St Mutien', 4, 100, CAST(50.434937 AS decimal(9,6)), CAST(4.766989 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Malonne en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Mutien', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:08:15.000', 121)),
			(120, CONVERT(date, '2026-10-29', 23), N'Malonne', 14, N'Marche St Mutien', 4, 100, CAST(50.434937 AS decimal(9,6)), CAST(4.766989 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Malonne en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M St Mutien', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:09:23.000', 121)),
			(121, CONVERT(date, '2026-12-06', 23), N'Thuin', 12, N'Marche Ste Barbe', 4, 100, CAST(50.322062 AS decimal(9,6)), CAST(4.145776 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 14, N'Evitez Thuin en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'M Ste Barbe', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:12:07.000', 121)),
			(122, CONVERT(date, '2026-12-06', 23), N'Bastogne', 106, N'Nuts Weekend', 4, 100, CAST(50.010734 AS decimal(9,6)), CAST(5.740153 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '22:30:00', 108), 72, N'Evitez Bastogne Mardasson Memorial en empruntant les itinéraires de contournements prévus ou assistez aux célébrations', N'Nuts Weekend', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 18:18:05.000', 121)),
			(123, CONVERT(date, '2026-10-18', 23), N'Villers-Poterie', 24, N'Tour Ste Rolende des Marcheurs', 4, 100, CAST(50.356266 AS decimal(9,6)), CAST(4.531074 AS decimal(9,6)), CONVERT(time(0),'08:30:00', 108), CONVERT(time(0),'23:59:59', 108), 24, N'Evitez Villers-Poterie en empruntant les itinéraires de contournements prévus ou vous êtes les bienvenus pour assister aux célébrations', N'T Ste Rolende des Mrs', CAST(1 AS bit), CONVERT(datetime2(3), '2026-05-08 10:13:31.000', 121)),
			(124, CONVERT(date, '2026-09-27', 23), N'Haut-Vent', 107, N'St Feuillen', 4, 100, CAST(50.385592 AS decimal(9,6)), CAST(4.665234 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '23:59:59', 108), 24, N'Evitez Haut-Vent en empruntant les itinéraires de contournements prévus ou vous êtes les bienvenus pour assister aux célébrations', N'M St Feuillen', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 10:18:29.000', 121)),
			(125, CONVERT(date, '2026-05-25', 23), N'Forchies-la-Marche', 108, N'Marche de la Vierge', 4, 100, CAST(50.433431 AS decimal(9,6)), CAST(4.278237 AS decimal(9,6)), CONVERT(time(0), '08:30:00', 108), CONVERT(time(0), '23:59:59', 108), 24, N'Evitez Forchies-la-Marche en empruntant les itinéraires de contournements prévus ou vous êtes les bienvenus pour assister aux célébrations', N'M de la Vierge', CAST(1 AS bit), CONVERT(datetime2, '2026-05-08 10:52:19.000', 121))
        ) AS V
        (
            Id,
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
            Active,
            CreatedAt
        )
    )
    MERGE dbo.CrowdCalendar AS T
    USING SourceCrowdCalendar AS S
        ON T.Id = S.Id
    WHEN MATCHED THEN
        UPDATE SET
            T.DateUtc = S.DateUtc,
            T.RegionCode = S.RegionCode,
            T.PlaceId = S.PlaceId,
            T.EventName = S.EventName,
            T.ExpectedLevel = S.ExpectedLevel,
            T.Confidence = S.Confidence,
            T.Latitude = S.Latitude,
            T.Longitude = S.Longitude,
            T.StartLocalTime = S.StartLocalTime,
            T.EndLocalTime = S.EndLocalTime,
            T.LeadHours = S.LeadHours,
            T.MessageTemplate = S.MessageTemplate,
            T.Tags = S.Tags,
            T.Active = S.Active
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            Id,
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
            Active,
            CreatedAt
        )
        VALUES
        (
            S.Id,
            S.DateUtc,
            S.RegionCode,
            S.PlaceId,
            S.EventName,
            S.ExpectedLevel,
            S.Confidence,
            S.Latitude,
            S.Longitude,
            S.StartLocalTime,
            S.EndLocalTime,
            S.LeadHours,
            S.MessageTemplate,
            S.Tags,
            S.Active,
            S.CreatedAt
        );

    PRINT 'POSTDEPLOY SEED: CrowdCalendar MERGE done';

    SET IDENTITY_INSERT dbo.CrowdCalendar OFF;

	DECLARE @MaxPlaceId INT = ISNULL((SELECT MAX(Id) FROM dbo.Place), 0);
    DECLARE @MaxCrowdCalendarId INT = ISNULL((SELECT MAX(Id) FROM dbo.CrowdCalendar), 0);

    DBCC CHECKIDENT ('dbo.Place', RESEED, @MaxPlaceId);
    DBCC CHECKIDENT ('dbo.CrowdCalendar', RESEED, @MaxCrowdCalendarId);

    PRINT 'POSTDEPLOY SEED COMPLETED';
    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    BEGIN TRY
        IF OBJECT_ID(N'dbo.Place', N'U') IS NOT NULL
            SET IDENTITY_INSERT dbo.Place OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    BEGIN TRY
        IF OBJECT_ID(N'dbo.CrowdCalendar', N'U') IS NOT NULL
            SET IDENTITY_INSERT dbo.CrowdCalendar OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    THROW;
END CATCH;
GO































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.