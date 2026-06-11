-- ============================================================
--  Detalle 3: roles Revisor y Aprobador + flujo de 2 pasos.
--  - Agrega los roles Revisor y Aprobador.
--  - Renombra el estado "Pendiente" a "Pendiente de revision" y
--    agrega "Pendiente de aprobacion".
--  - Convierte al usuario Director en Aprobador y crea el usuario Revisor.
--  EJECUTAR UNA SOLA VEZ contra la base CentralDB.
-- ============================================================

-- 1) Roles nuevos (Nivel solo ordena el combo al crear usuarios).
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Aprobador')
    INSERT INTO dbo.Roles (Nombre, Nivel, Descripcion)
    VALUES ('Aprobador', 15, 'Da la aprobacion final a los documentos ya revisados.');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Revisor')
    INSERT INTO dbo.Roles (Nombre, Nivel, Descripcion)
    VALUES ('Revisor', 18, 'Revisa los documentos subidos y los pasa al Aprobador.');
GO

-- 2) Estados nuevos. Renombramos "Pendiente" -> "Pendiente de revision"
--    (asi los documentos que ya estaban pendientes entran a revision) y
--    agregamos "Pendiente de aprobacion".
IF EXISTS (SELECT 1 FROM dbo.Estados WHERE Nombre = 'Pendiente')
    UPDATE dbo.Estados
       SET Nombre = 'Pendiente de revision',
           Descripcion = 'Esperando que el Revisor lo revise.'
     WHERE Nombre = 'Pendiente';

IF NOT EXISTS (SELECT 1 FROM dbo.Estados WHERE Nombre = 'Pendiente de revision')
    INSERT INTO dbo.Estados (Nombre, Descripcion)
    VALUES ('Pendiente de revision', 'Esperando que el Revisor lo revise.');

IF NOT EXISTS (SELECT 1 FROM dbo.Estados WHERE Nombre = 'Pendiente de aprobacion')
    INSERT INTO dbo.Estados (Nombre, Descripcion)
    VALUES ('Pendiente de aprobacion', 'Revisado; esperando la aprobacion final.');
GO

-- 3) Convertir al usuario Director demo en Aprobador.
UPDATE u
   SET u.IdRol = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Aprobador')
  FROM dbo.Usuarios u
 WHERE u.Correo = 'director@metalmex.com';
GO

-- 4) Crear el usuario Revisor demo (misma empresa que el Director y misma
--    contrasena Demo123!, copiando un hash valido de un usuario existente).
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo = 'revisor@metalmex.com')
    INSERT INTO dbo.Usuarios (IdEmpresa, IdRol, NombreCompleto, Correo, ContrasenaHash, Activo, FechaCreacion)
    SELECT
        (SELECT TOP 1 IdEmpresa FROM dbo.Usuarios WHERE Correo = 'director@metalmex.com'),
        (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Revisor'),
        'Revisor Metalmex',
        'revisor@metalmex.com',
        (SELECT TOP 1 ContrasenaHash FROM dbo.Usuarios WHERE Correo = 'director@metalmex.com'),
        1,
        SYSUTCDATETIME();
GO

-- 5) Quitar el rol "Director" si ya nadie lo usa (queda reemplazado por
--    Revisor + Aprobador). Es opcional y solo se borra si esta libre.
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios u JOIN dbo.Roles r ON r.IdRol = u.IdRol WHERE r.Nombre = 'Director')
    DELETE FROM dbo.Roles WHERE Nombre = 'Director';
GO

-- ----- Verificacion -----
PRINT 'Roles:';
SELECT Nombre, Nivel FROM dbo.Roles ORDER BY Nivel;
PRINT 'Estados:';
SELECT Nombre FROM dbo.Estados ORDER BY IdEstado;
PRINT 'Usuarios y su rol:';
SELECT u.Correo, r.Nombre AS Rol, u.Activo
  FROM dbo.Usuarios u JOIN dbo.Roles r ON r.IdRol = u.IdRol
 ORDER BY u.Correo;
GO
