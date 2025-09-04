USE CitizenHackathon2025Db;
GO

-- 1) Supprimer la contrainte unique
ALTER TABLE dbo.WeatherForecast
DROP CONSTRAINT [UQ_WeatherForecast_Summary];
GO

-- 2) Élargir la colonne Summary
ALTER TABLE dbo.WeatherForecast
ALTER COLUMN Summary NVARCHAR(256) NOT NULL;
GO

-- 3) Recréer la contrainte unique (même nom)
ALTER TABLE dbo.WeatherForecast
ADD CONSTRAINT [UQ_WeatherForecast_Summary] UNIQUE (Summary);
GO