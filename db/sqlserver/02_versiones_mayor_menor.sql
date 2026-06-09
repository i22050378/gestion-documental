-- ============================================================
--  Agrega numeracion mayor.menor a las versiones (1.0, 1.1, 2.0, ...)
--  EJECUTAR UNA SOLA VEZ contra la base CentralDB.
-- ============================================================

IF COL_LENGTH('dbo.Versiones', 'VersionMajor') IS NULL
    ALTER TABLE dbo.Versiones ADD VersionMajor INT NOT NULL CONSTRAINT DF_Versiones_Major DEFAULT 1;

IF COL_LENGTH('dbo.Versiones', 'VersionMinor') IS NULL
    ALTER TABLE dbo.Versiones ADD VersionMinor INT NOT NULL CONSTRAINT DF_Versiones_Minor DEFAULT 0;
GO

-- Backfill para los documentos de prueba que ya existian:
--   parte entera = el numero secuencial que tenian; parte decimal = 0.
UPDATE dbo.Versiones SET VersionMajor = NumeroVersion, VersionMinor = 0;
GO

SELECT IdVersion, IdDocumento, NumeroVersion, VersionMajor, VersionMinor FROM dbo.Versiones ORDER BY IdDocumento, NumeroVersion;
GO
