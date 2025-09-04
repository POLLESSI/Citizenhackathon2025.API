USE CitizenHackathon2025Db;
GO

-- 1) Vérifier les contraintes pour connaître le nom exact
EXEC sp_helpconstraint 'dbo.WeatherForecast', 'nomsg';
GO

-- 2) Supprimer la contrainte UNIQUE (le nom doit correspondre au tien)
ALTER TABLE dbo.WeatherForecast
DROP CONSTRAINT UQ_WeatherForecast_Summary;
GO

-- 3) (Optionnel) Remettre un index NON UNIQUE pour les perfs
CREATE NONCLUSTERED INDEX IX_WeatherForecast_Summary
ON dbo.WeatherForecast(Summary);
GO
