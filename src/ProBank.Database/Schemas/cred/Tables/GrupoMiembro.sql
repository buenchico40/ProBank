-- CRE-02: usuarios de ProBank que integran un grupo de acceso.
CREATE TABLE [cred].[GrupoMiembro]
(
    GrupoId   INT NOT NULL CONSTRAINT FK_GrupoMiembro_Grupo REFERENCES cred.GrupoAcceso (GrupoId),
    UsuarioId INT NOT NULL CONSTRAINT FK_GrupoMiembro_Usuario REFERENCES seg.Usuario (UsuarioId),
    Desde     DATETIME2(0) NOT NULL CONSTRAINT DF_GrupoMiembro_Desde DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_GrupoMiembro PRIMARY KEY (GrupoId, UsuarioId)
);
