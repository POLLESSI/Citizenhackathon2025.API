USE CitizenHackathon2025Db;
GO

-- 1) Supprimer les contraintes UNIQUE problématiques
ALTER TABLE dbo.WeatherForecast DROP CONSTRAINT [UQ_WeatherForecast_Summary];
ALTER TABLE dbo.WeatherForecast DROP CONSTRAINT [UQ_WeatherForecast_TemperatureC];
ALTER TABLE dbo.WeatherForecast DROP CONSTRAINT [UQ_WeatherForecast_TemperatureF];
ALTER TABLE dbo.WeatherForecast DROP CONSTRAINT [UQ_WeatherForecast_DateWeather];
GO

-- 2) (Optionnel) Remettre des index NON-UNIQUE pour les perfs
CREATE NONCLUSTERED INDEX IX_WeatherForecast_Summary        ON dbo.WeatherForecast(Summary);
CREATE NONCLUSTERED INDEX IX_WeatherForecast_TemperatureC   ON dbo.WeatherForecast(TemperatureC);
CREATE NONCLUSTERED INDEX IX_WeatherForecast_TemperatureF   ON dbo.WeatherForecast(TemperatureF);
CREATE NONCLUSTERED INDEX IX_WeatherForecast_DateWeather    ON dbo.WeatherForecast(DateWeather);
GO

-- 3) Vérifier : il ne doit rester que la PK et le DEFAULT
EXEC sp_helpconstraint 'dbo.WeatherForecast', 'nomsg';