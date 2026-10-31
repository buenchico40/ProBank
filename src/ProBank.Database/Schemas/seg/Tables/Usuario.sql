CREATE TABLE [seg].[Usuario]
(
UsuarioId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
    -- Identificador opaco para la API y la Web (anti-IDOR): el UsuarioId no sale de la base.
    Token            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Usuario_Token DEFAULT (NEWID()) CONSTRAINT UQ_Usuario_Token UNIQUE,
    EntraObjectId    UNIQUEIDENTIFIER NULL,
    Usuario          VARCHAR(60)  NOT NULL CONSTRAINT UQ_Usuario_Usuario UNIQUE,
    Nombre           NVARCHAR(150) NOT NULL,
    Correo           VARCHAR(256) NULL,
    RolPredeterminado VARCHAR(60) NULL,
    EsCuentaLocal    BIT NOT NULL CONSTRAINT DF_Usuario_Local DEFAULT (0),
    ClaveHash        VARBINARY(64) NULL,
    ClaveSal         VARBINARY(32) NULL,
    ClaveCambiada    DATETIME2(0) NULL,
    -- SEG-07: tras crear o restablecer la clave, el usuario debe cambiarla al ingresar.
    DebeCambiarClave BIT NOT NULL CONSTRAINT DF_Usuario_DebeCambiar DEFAULT (0),
    IpPermitida      VARCHAR(45) NULL,
    Estado           VARCHAR(20) NOT NULL CONSTRAINT DF_Usuario_Estado DEFAULT ('Activo'),
    UltimaConexion   DATETIME2(0) NULL,
    UltimaIp         VARCHAR(45) NULL,
    ConexionAnterior DATETIME2(0) NULL,
    IpAnterior       VARCHAR(45) NULL,
    -- SEG-06: tras N intentos fallidos la cuenta queda bloqueada (seg.ParametroAcceso, seg.usp_Usuario_RegistrarIngreso).
    IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuario_Intentos DEFAULT (0),
    BloqueadoHasta   DATETIME2(0) NULL,
    -- SEG-07: viaja en el token; al subirlo (revocar, cambiar clave, roles o estado) las sesiones abiertas dejan de valer.
    SelloSesion      INT NOT NULL CONSTRAINT DF_Usuario_Sello DEFAULT (1)
);
GO

-- Único solo cuando existe: las cuentas locales (ProBank.Setup) y los usuarios aún no vinculados no tienen objeto de Entra ID.
CREATE UNIQUE NONCLUSTERED INDEX UX_Usuario_EntraObjectId ON seg.Usuario (EntraObjectId) WHERE EntraObjectId IS NOT NULL;
