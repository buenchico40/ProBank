-- CRE-02: cada vez que alguien muestra o copia una credencial (además queda en aud.Auditoria).
CREATE TABLE [cred].[CredencialAcceso]
(
    AccesoId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CredencialAcceso PRIMARY KEY,
    CuentaId  INT NULL CONSTRAINT FK_CredAcceso_Cuenta REFERENCES cred.CuentaTecnica (CuentaId),
    SecretoId INT NULL CONSTRAINT FK_CredAcceso_Secreto REFERENCES cred.Secreto (SecretoId),
    Version   INT NOT NULL,
    Accion    VARCHAR(10) NOT NULL CONSTRAINT CK_CredAcceso_Accion CHECK (Accion IN ('Mostrar', 'Copiar')),
    UsuarioId INT NOT NULL CONSTRAINT FK_CredAcceso_Usuario REFERENCES seg.Usuario (UsuarioId),
    Fecha     DATETIME2(0) NOT NULL CONSTRAINT DF_CredAcceso_Fecha DEFAULT (SYSDATETIME()),
    Ip        VARCHAR(45) NULL,
    CONSTRAINT CK_CredAcceso_Duenio CHECK ((CuentaId IS NULL AND SecretoId IS NOT NULL) OR (CuentaId IS NOT NULL AND SecretoId IS NULL))
);
GO
CREATE INDEX IX_CredAcceso_Cuenta ON cred.CredencialAcceso (CuentaId, Fecha) WHERE CuentaId IS NOT NULL;
GO
CREATE INDEX IX_CredAcceso_Secreto ON cred.CredencialAcceso (SecretoId, Fecha) WHERE SecretoId IS NOT NULL;
