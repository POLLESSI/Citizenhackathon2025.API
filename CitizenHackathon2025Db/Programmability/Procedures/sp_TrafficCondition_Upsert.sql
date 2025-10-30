CREATE PROCEDURE dbo.sp_TrafficCondition_Upsert
    @Latitude        DECIMAL(9, 2),
    @Longitude       DECIMAL(9, 3),
    @DateCondition   DATETIME2(0),
    @CongestionLevel NVARCHAR(16),
    @IncidentType    NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Archive any existing active row for this location
    UPDATE dbo.TrafficCondition
       SET Active = 0
     WHERE Active = 1
       AND Latitude  = @Latitude
       AND Longitude = @Longitude;

    -- 2) Insert the fresh row
    INSERT INTO dbo.TrafficCondition
        (Latitude, Longitude, DateCondition, CongestionLevel, IncidentType, Active)
    OUTPUT INSERTED.*
    VALUES
        (@Latitude, @Longitude, @DateCondition, @CongestionLevel, @IncidentType, 1);
END
GO











































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.