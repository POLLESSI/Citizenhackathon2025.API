CREATE PROCEDURE dbo.sp_CrowdInfoAntenna_Upsert
  @Name nvarchar(64),
  @Latitude decimal(9,6),
  @Longitude decimal(9,6),
  @Description nvarchar(256) = null,
  @MaxCapacity int = null,
  @Active bit = 1
AS
BEGIN
  SET NOCOUNT ON;

  -- Match by coordinates (dev): pay attention to decimals -> round on the C# side first
  IF EXISTS (SELECT 1 FROM dbo.CrowdInfoAntenna WHERE Latitude=@Latitude AND Longitude=@Longitude)
  BEGIN
    UPDATE dbo.CrowdInfoAntenna
      SET Name=@Name, Description=@Description, MaxCapacity=@MaxCapacity, Active=@Active
    WHERE Latitude=@Latitude AND Longitude=@Longitude;

    SELECT Id FROM dbo.CrowdInfoAntenna WHERE Latitude=@Latitude AND Longitude=@Longitude;
    RETURN;
  END

  INSERT dbo.CrowdInfoAntenna (Name, Latitude, Longitude, Description, MaxCapacity, Active)
  VALUES (@Name, @Latitude, @Longitude, @Description, @MaxCapacity, @Active);

  SELECT SCOPE_IDENTITY() AS Id;
END
