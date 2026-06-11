-- ============================================================
--  Detalle 3 (arreglo): permitir los nuevos tipos de revision
--  del flujo de 2 pasos en la tabla Revisiones.
--  El Revisor genera 'REVISION_OK' (al pasar al Aprobador) y
--  'RECHAZO_REVISION' (al rechazar). La restriccion CHECK vieja
--  solo permitia los tipos anteriores, por eso fallaba al guardar.
--  EJECUTAR UNA SOLA VEZ contra la base CentralDB.
-- ============================================================

-- 1) Quitar la restriccion vieja (si existe).
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Revisiones_Tipo')
    ALTER TABLE dbo.Revisiones DROP CONSTRAINT CK_Revisiones_Tipo;
GO

-- 2) Crear la restriccion nueva con TODOS los tipos que usa el sistema
--    (los de antes + los dos nuevos del flujo de revision).
--    WITH NOCHECK evita que falle por datos ya existentes; la regla se
--    sigue aplicando a los registros nuevos.
ALTER TABLE dbo.Revisiones WITH NOCHECK
    ADD CONSTRAINT CK_Revisiones_Tipo CHECK (Tipo IN (
        'APROBACION',
        'RECHAZO',
        'REPORTE_ERROR',
        'PREREVISION',
        'REVISION_OK',
        'RECHAZO_REVISION'
    ));
GO

-- 3) Verificacion: muestra la definicion de la regla ya actualizada.
SELECT name AS Restriccion, definition AS Definicion
  FROM sys.check_constraints
 WHERE name = 'CK_Revisiones_Tipo';
GO
