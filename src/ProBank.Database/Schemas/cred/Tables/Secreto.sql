-- CRE-01: secreto (cadena de conexión, clave de API, secreto de cliente, token SAS). Su valor va en cred.CredencialVersion.
CREATE TABLE [cred].[Secreto]
(
    SecretoId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Secreto PRIMARY KEY,
    Token          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Secreto_Token DEFAULT (NEWID()) CONSTRAINT UQ_Secreto_Token UNIQUE,
    Nombre         NVARCHAR(150) NOT NULL,
    Tipo           NVARCHAR(60)  NOT NULL CONSTRAINT CK_Secreto_Tipo CHECK (Tipo IN (N'Cadena de conexión', N'Clave de API',
                   N'Secreto de cliente (Entra ID)', N'Token SAS')),
    Almacen        NVARCHAR(100) NOT NULL,
    Sistema        NVARCHAR(150) NOT NULL,
    GrupoId        INT           NOT NULL CONSTRAINT FK_Secreto_Grupo REFERENCES cred.GrupoAcceso (GrupoId),
    DiasAviso      INT           NOT NULL CONSTRAINT CK_Secreto_Aviso CHECK (DiasAviso BETWEEN 1 AND 365),
    Expira         DATE          NULL,
    UltimaRotacion DATE          NULL,
    Activo         BIT           NOT NULL CONSTRAINT DF_Secreto_Activo DEFAULT (1),
    FechaRegistro  DATETIME2(0)  NOT NULL CONSTRAINT DF_Secreto_Registro DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_Secreto UNIQUE (Almacen, Nombre)
);
