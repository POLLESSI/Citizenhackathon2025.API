ALTER TABLE dbo.CrowdInfoAntenna
ADD CONSTRAINT CK_CrowdInfoAntenna_Lat CHECK (Latitude BETWEEN -90 AND 90);

ALTER TABLE dbo.CrowdInfoAntenna
ADD CONSTRAINT CK_CrowdInfoAntenna_Lng CHECK (Longitude BETWEEN -180 AND 180);

ALTER TABLE dbo.CrowdInfoAntenna
ADD CONSTRAINT DF_CrowdInfoAntenna_Active DEFAULT(1) FOR Active;