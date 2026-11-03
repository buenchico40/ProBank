-- CRE-02: grupo de acceso de la Bóveda. Solo sus miembros ven, copian y cambian las credenciales asignadas al grupo.
CREATE TABLE [cred].[GrupoAcceso]
(
    GrupoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GrupoAcceso PRIMARY KEY,
    Token         UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_GrupoAcceso_Token DEFAULT (NEWID()) CONSTRAINT UQ_GrupoAcceso_Token UNIQUE,
    Codigo        VARCHAR(10)   NOT NULL CONSTRAINT UQ_GrupoAcceso_Codigo UNIQUE,
    Nombre        NVARCHAR(100) NOT NULL CONSTRAINT UQ_GrupoAcceso UNIQUE,
    Descripcion   NVARCHAR(300) NULL,
    Estado        VARCHAR(10)   NOT NULL CONSTRAINT DF_GrupoAcceso_Estado DEFAULT ('Activo')
                  CONSTRAINT CK_GrupoAcceso_Estado CHECK (Estado IN ('Activo', 'Inactivo')),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_GrupoAcceso_Registro DEFAULT (SYSDATETIME())
);
