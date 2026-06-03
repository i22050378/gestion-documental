/* ============================================================
   Modulo Central - SQL Server
   Esquema + datos semilla
   Base de datos: CentralDB
   Se puede ejecutar varias veces sin error (idempotente).
   ============================================================ */

IF DB_ID('CentralDB') IS NULL
    CREATE DATABASE CentralDB;
GO

USE CentralDB;
GO

/* ------------------------------------------------------------
   TABLAS DE CATALOGO (lookup)
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Roles','U') IS NULL
CREATE TABLE dbo.Roles (
    IdRol       INT IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre      NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE,
    Nivel       INT           NOT NULL,   -- jerarquia: 0=Admin (mas alto) ... 3=Empleado
    Descripcion NVARCHAR(200) NULL
);
GO

IF OBJECT_ID('dbo.Estados','U') IS NULL
CREATE TABLE dbo.Estados (
    IdEstado    INT IDENTITY(1,1) CONSTRAINT PK_Estados PRIMARY KEY,
    Nombre      NVARCHAR(40)  NOT NULL CONSTRAINT UQ_Estados_Nombre UNIQUE,
    Descripcion NVARCHAR(200) NULL
);
GO

IF OBJECT_ID('dbo.Categorias','U') IS NULL
CREATE TABLE dbo.Categorias (
    IdCategoria INT IDENTITY(1,1) CONSTRAINT PK_Categorias PRIMARY KEY,
    Nombre      NVARCHAR(60)  NOT NULL CONSTRAINT UQ_Categorias_Nombre UNIQUE,
    Descripcion NVARCHAR(200) NULL
);
GO

/* ------------------------------------------------------------
   EMPRESAS Y USUARIOS
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Empresas','U') IS NULL
CREATE TABLE dbo.Empresas (
    IdEmpresa     INT IDENTITY(1,1) CONSTRAINT PK_Empresas PRIMARY KEY,
    Nombre        NVARCHAR(150) NOT NULL CONSTRAINT UQ_Empresas_Nombre UNIQUE,
    Activo        BIT NOT NULL CONSTRAINT DF_Empresas_Activo DEFAULT 1,   -- borrado logico
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Empresas_Fecha DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID('dbo.Usuarios','U') IS NULL
CREATE TABLE dbo.Usuarios (
    IdUsuario      INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    IdEmpresa      INT NULL,            -- NULL = Admin global (no pertenece a una empresa)
    IdRol          INT NOT NULL,
    NombreCompleto NVARCHAR(150) NOT NULL,
    Correo         NVARCHAR(150) NOT NULL CONSTRAINT UQ_Usuarios_Correo UNIQUE,
    ContrasenaHash NVARCHAR(255) NOT NULL,   -- siempre cifrada, nunca en texto plano
    Activo         BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT 1,
    FechaCreacion  DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT SYSUTCDATETIME(),
    UltimoAcceso   DATETIME2 NULL,
    CONSTRAINT FK_Usuarios_Empresas FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas(IdEmpresa),
    CONSTRAINT FK_Usuarios_Roles    FOREIGN KEY (IdRol)     REFERENCES dbo.Roles(IdRol)
);
GO

/* ------------------------------------------------------------
   DOCUMENTOS Y VERSIONES
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Documentos','U') IS NULL
CREATE TABLE dbo.Documentos (
    IdDocumento      INT IDENTITY(1,1) CONSTRAINT PK_Documentos PRIMARY KEY,
    IdEmpresa        INT NOT NULL,
    IdCategoria      INT NOT NULL,
    IdUsuarioCreador INT NOT NULL,
    Titulo           NVARCHAR(200) NOT NULL,
    Descripcion      NVARCHAR(500) NULL,
    Activo           BIT NOT NULL CONSTRAINT DF_Documentos_Activo DEFAULT 1,
    FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_Documentos_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Documentos_Empresas   FOREIGN KEY (IdEmpresa)        REFERENCES dbo.Empresas(IdEmpresa),
    CONSTRAINT FK_Documentos_Categorias FOREIGN KEY (IdCategoria)      REFERENCES dbo.Categorias(IdCategoria),
    CONSTRAINT FK_Documentos_Usuarios   FOREIGN KEY (IdUsuarioCreador) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

IF OBJECT_ID('dbo.Versiones','U') IS NULL
CREATE TABLE dbo.Versiones (
    IdVersion         INT IDENTITY(1,1) CONSTRAINT PK_Versiones PRIMARY KEY,
    IdDocumento       INT NOT NULL,
    NumeroVersion     INT NOT NULL,          -- 1, 2, 3, ...
    IdEstado          INT NOT NULL,
    IdUsuarioSubio    INT NOT NULL,
    NombreArchivo     NVARCHAR(255)  NOT NULL,
    Extension         NVARCHAR(20)   NOT NULL,
    TamanoBytes       BIGINT         NOT NULL,
    Archivo           VARBINARY(MAX) NOT NULL,   -- el archivo se guarda aqui
    FechaSubida       DATETIME2 NOT NULL CONSTRAINT DF_Versiones_Fecha DEFAULT SYSUTCDATETIME(),
    IdUsuarioRevisor  INT NULL,               -- el Director que reviso (si aplica)
    FechaRevision     DATETIME2 NULL,
    Activo            BIT NOT NULL CONSTRAINT DF_Versiones_Activo DEFAULT 1,
    CONSTRAINT FK_Versiones_Documentos FOREIGN KEY (IdDocumento)      REFERENCES dbo.Documentos(IdDocumento),
    CONSTRAINT FK_Versiones_Estados    FOREIGN KEY (IdEstado)         REFERENCES dbo.Estados(IdEstado),
    CONSTRAINT FK_Versiones_UsuarioSub FOREIGN KEY (IdUsuarioSubio)   REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_Versiones_UsuarioRev FOREIGN KEY (IdUsuarioRevisor) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT UQ_Versiones_DocNumero  UNIQUE (IdDocumento, NumeroVersion)
);
GO

/* ------------------------------------------------------------
   REVISIONES, NOTIFICACIONES Y BITACORA
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Revisiones','U') IS NULL
CREATE TABLE dbo.Revisiones (
    IdRevision  INT IDENTITY(1,1) CONSTRAINT PK_Revisiones PRIMARY KEY,
    IdVersion   INT NOT NULL,
    IdUsuario   INT NOT NULL,
    Tipo        NVARCHAR(30)   NOT NULL,   -- APROBACION, RECHAZO, REPORTE_ERROR, PREREVISION, COMENTARIO
    Comentario  NVARCHAR(1000) NULL,
    FechaHora   DATETIME2 NOT NULL CONSTRAINT DF_Revisiones_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Revisiones_Versiones FOREIGN KEY (IdVersion) REFERENCES dbo.Versiones(IdVersion),
    CONSTRAINT FK_Revisiones_Usuarios  FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Revisiones_Tipo CHECK (Tipo IN ('APROBACION','RECHAZO','REPORTE_ERROR','PREREVISION','COMENTARIO'))
);
GO

IF OBJECT_ID('dbo.Notificaciones','U') IS NULL
CREATE TABLE dbo.Notificaciones (
    IdNotificacion INT IDENTITY(1,1) CONSTRAINT PK_Notificaciones PRIMARY KEY,
    IdUsuario      INT NOT NULL,
    Mensaje        NVARCHAR(500) NOT NULL,
    IdVersion      INT NULL,
    Leida          BIT NOT NULL CONSTRAINT DF_Notif_Leida DEFAULT 0,
    FechaCreacion  DATETIME2 NOT NULL CONSTRAINT DF_Notif_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Notif_Usuarios  FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_Notif_Versiones FOREIGN KEY (IdVersion) REFERENCES dbo.Versiones(IdVersion)
);
GO

IF OBJECT_ID('dbo.Bitacora','U') IS NULL
CREATE TABLE dbo.Bitacora (
    IdBitacora  INT IDENTITY(1,1) CONSTRAINT PK_Bitacora PRIMARY KEY,
    IdUsuario   INT NULL,               -- NULL solo para intentos de login fallidos
    Accion      NVARCHAR(50)  NOT NULL, -- LOGIN, LOGOUT, ABRIR, DESCARGAR, SUBIR, APROBAR, RECHAZAR, ...
    Entidad     NVARCHAR(50)  NULL,     -- DOCUMENTO, VERSION, USUARIO, ...
    IdEntidad   INT           NULL,     -- id del elemento afectado
    Detalle     NVARCHAR(500) NULL,
    DireccionIP NVARCHAR(45)  NULL,
    FechaHora   DATETIME2 NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Bitacora_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* ------------------------------------------------------------
   INDICES (consultas mas rapidas)
   ------------------------------------------------------------ */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Usuarios_Empresa')
    CREATE INDEX IX_Usuarios_Empresa   ON dbo.Usuarios(IdEmpresa);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Documentos_Empresa')
    CREATE INDEX IX_Documentos_Empresa ON dbo.Documentos(IdEmpresa);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Versiones_Documento')
    CREATE INDEX IX_Versiones_Documento ON dbo.Versiones(IdDocumento);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Versiones_Estado')
    CREATE INDEX IX_Versiones_Estado    ON dbo.Versiones(IdEstado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Bitacora_Usuario')
    CREATE INDEX IX_Bitacora_Usuario    ON dbo.Bitacora(IdUsuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Notif_Usuario')
    CREATE INDEX IX_Notif_Usuario       ON dbo.Notificaciones(IdUsuario);
GO

/* ============================================================
   DATOS SEMILLA
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
INSERT INTO dbo.Roles (Nombre, Nivel, Descripcion) VALUES
 ('Admin',      0, 'Administrador global del sistema'),
 ('Director',   1, 'Aprueba o rechaza documentos de su empresa'),
 ('Supervisor', 2, 'Crea y sube documentos'),
 ('Empleado',   3, 'Consulta documentos y prerrevisa');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Estados)
INSERT INTO dbo.Estados (Nombre, Descripcion) VALUES
 ('Borrador',  'Documento en elaboracion, aun no enviado'),
 ('Pendiente', 'Enviado, esperando revision del Director'),
 ('Aprobado',  'Aprobado y vigente'),
 ('Rechazado', 'Rechazado, requiere correccion'),
 ('Obsoleto',  'Version aprobada anterior, ya no vigente');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categorias)
INSERT INTO dbo.Categorias (Nombre, Descripcion) VALUES
 ('Manual',        'Manuales de calidad'),
 ('Procedimiento', 'Procedimientos operativos'),
 ('Registro',      'Registros y formatos'),
 ('Auditoria',     'Documentos de auditoria');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE Nombre = 'Metalmex')
INSERT INTO dbo.Empresas (Nombre) VALUES ('Metalmex');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios)
BEGIN
    DECLARE @idMetalmex INT = (SELECT IdEmpresa FROM dbo.Empresas WHERE Nombre = 'Metalmex');
    DECLARE @rAdmin INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Admin');
    DECLARE @rDir   INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Director');
    DECLARE @rSup   INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Supervisor');
    DECLARE @rEmp   INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Empleado');

    -- ContrasenaHash va con un marcador: el cifrado real lo genera
    -- el modulo .NET en la Fase 3 (todavia no hay donde iniciar sesion).
    INSERT INTO dbo.Usuarios (IdEmpresa, IdRol, NombreCompleto, Correo, ContrasenaHash) VALUES
     (NULL,        @rAdmin, 'Administrador General', 'admin@sistema.com',       'PENDIENTE_HASH'),
     (@idMetalmex, @rDir,   'Director Metalmex',     'director@metalmex.com',   'PENDIENTE_HASH'),
     (@idMetalmex, @rSup,   'Supervisor Metalmex',   'supervisor@metalmex.com', 'PENDIENTE_HASH'),
     (@idMetalmex, @rEmp,   'Empleado Metalmex',     'empleado@metalmex.com',   'PENDIENTE_HASH');
END
GO

PRINT 'CentralDB lista: esquema y datos semilla cargados.';
GO
