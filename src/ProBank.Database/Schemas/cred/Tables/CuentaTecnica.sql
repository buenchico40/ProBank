-- CRE-01: cuenta técnica (servicio de Entra ID, AD on-premise, base de datos o identidad administrada). Su contraseña va en
-- cred.CredencialVersion. La identidad administrada no tiene contraseña, grupo ni rotación.
CREATE TABLE [cred].[CuentaTecnica]
(
    CuentaId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CuentaTecnica PRIMARY KEY,
    Token           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CuentaTecnica_Token DEFAULT (NEWID()) CONSTRAINT UQ_CuentaTecnica_Token UNIQUE,
    Nombre          NVARCHAR(150) NOT NULL CONSTRAINT UQ_CuentaTecnica UNIQUE,
    Tipo            NVARCHAR(60)  NOT NULL CONSTRAINT CK_CuentaTecnica_Tipo CHECK (Tipo IN (N'Cuenta de servicio de Entra ID',
                    N'Cuenta de servicio del AD (on-premise)', N'Cuenta de base de datos', N'Identidad administrada de Azure')),
    Sistema         NVARCHAR(150) NOT NULL,
    Responsable     NVARCHAR(100) NOT NULL,
    Privilegios     NVARCHAR(300) NULL,
    GrupoId         INT           NULL CONSTRAINT FK_CuentaTecnica_Grupo REFERENCES cred.GrupoAcceso (GrupoId),
    ProximaRotacion DATE          NULL,
    UltimaRotacion  DATE          NULL,
    Activo          BIT           NOT NULL CONSTRAINT DF_CuentaTecnica_Activo DEFAULT (1),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_CuentaTecnica_Registro DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_CuentaTecnica_Grupo CHECK ((Tipo = N'Identidad administrada de Azure' AND GrupoId IS NULL AND ProximaRotacion IS NULL)
                                             OR (Tipo <> N'Identidad administrada de Azure' AND GrupoId IS NOT NULL))
);
