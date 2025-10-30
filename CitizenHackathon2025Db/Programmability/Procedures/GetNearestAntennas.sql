CREATE PROCEDURE [dbo].[GetNearestAntennas]
    @lat FLOAT,
    @lon FLOAT,
    @radiusMeters INT = 5000,
    @top INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @pt geography = geography::Point(@lat, @lon, 4326);

    SELECT TOP(@top)
        a.Id,
        a.Name,
        a.Latitude,
        a.Longitude,
        a.Active,
        a.CreatedUtc,
        a.Description,
        a.GeoLocation.STDistance(@pt) AS DistanceMeters
    FROM dbo.CrowdInfoAntenna a
    WHERE a.Active = 1
      AND a.GeoLocation.STDistance(@pt) <= @radiusMeters
    ORDER BY DistanceMeters ASC;
END;
GO
