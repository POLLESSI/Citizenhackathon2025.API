ALTER TABLE dbo.GptInteractions
ADD
    EventId           INT           NULL,
    CrowdInfoId       INT           NULL,
    PlaceId           INT           NULL,
    TrafficConditionId INT          NULL,
    WeatherForecastId INT           NULL,
    Latitude          DECIMAL(9, 6) NULL,
    Longitude         DECIMAL(9, 6) NULL,
    SourceType        NVARCHAR(32)  NULL;
GO

ALTER TABLE dbo.GptInteractions
ADD CONSTRAINT FK_Gpt_Event
    FOREIGN KEY (EventId) REFERENCES dbo.Events(Id);

ALTER TABLE dbo.GptInteractions
ADD CONSTRAINT FK_Gpt_Crowd
    FOREIGN KEY (CrowdInfoId) REFERENCES dbo.CrowdInfo(Id);

ALTER TABLE dbo.GptInteractions
ADD CONSTRAINT FK_Gpt_Place
    FOREIGN KEY (PlaceId) REFERENCES dbo.Places(Id);

ALTER TABLE dbo.GptInteractions
ADD CONSTRAINT FK_Gpt_Traffic
    FOREIGN KEY (TrafficConditionId) REFERENCES dbo.TrafficCondition(Id);

ALTER TABLE dbo.GptInteractions
ADD CONSTRAINT FK_Gpt_Weather
    FOREIGN KEY (WeatherForecastId) REFERENCES dbo.WeatherForecast(Id);
GO

ALTER PROCEDURE dbo.sp_GptInteraction_Upsert
    @Prompt             NVARCHAR(MAX),
    @PromptHash         NVARCHAR(64),
    @Response           NVARCHAR(MAX),
    @EventId            INT = NULL,
    @CrowdInfoId        INT = NULL,
    @PlaceId            INT = NULL,
    @TrafficConditionId INT = NULL,
    @WeatherForecastId  INT = NULL,
    @Latitude           DECIMAL(9,6) = NULL,
    @Longitude          DECIMAL(9,6) = NULL,
    @SourceType         NVARCHAR(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

    MERGE dbo.GptInteractions AS T
    USING (SELECT @PromptHash AS PromptHash) AS S
        ON (T.PromptHash = S.PromptHash)
    WHEN MATCHED THEN
        UPDATE SET 
            T.Prompt             = @Prompt,
            T.Response           = @Response,
            T.CreatedAt          = @Now,
            T.Active             = 1,
            T.DateDeleted        = NULL,
            T.EventId            = @EventId,
            T.CrowdInfoId        = @CrowdInfoId,
            T.PlaceId            = @PlaceId,
            T.TrafficConditionId = @TrafficConditionId,
            T.WeatherForecastId  = @WeatherForecastId,
            T.Latitude           = @Latitude,
            T.Longitude          = @Longitude,
            T.SourceType         = @SourceType
    WHEN NOT MATCHED THEN
        INSERT (Prompt, PromptHash, Response, CreatedAt, Active,
                EventId, CrowdInfoId, PlaceId, TrafficConditionId,
                WeatherForecastId, Latitude, Longitude, SourceType)
        VALUES (@Prompt, @PromptHash, @Response, @Now, 1,
                @EventId, @CrowdInfoId, @PlaceId, @TrafficConditionId,
                @WeatherForecastId, @Latitude, @Longitude, @SourceType);

    SELECT TOP (1) *
    FROM dbo.GptInteractions
    WHERE PromptHash = @PromptHash;
END;
GO
