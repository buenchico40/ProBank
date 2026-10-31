CREATE TABLE [aud].[Auditoria]
(
AuditoriaId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Auditoria PRIMARY KEY,
    Fecha       DATETIME2(0) NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT (SYSDATETIME()),
    UsuarioId   INT NULL,
    RolActivo   VARCHAR(60) NULL,
    Modulo      VARCHAR(60) NOT NULL,
    Accion      NVARCHAR(200) NOT NULL,
    Detalle     NVARCHAR(2000) NULL,
    Ip          VARCHAR(45) NULL
);
