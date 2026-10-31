CREATE TABLE [seg].[UsuarioRol]
(
UsuarioId INT NOT NULL CONSTRAINT FK_UsuarioRol_Usuario REFERENCES seg.Usuario (UsuarioId),
    RolId     INT NOT NULL CONSTRAINT FK_UsuarioRol_Rol REFERENCES seg.Rol (RolId),
    CONSTRAINT PK_UsuarioRol PRIMARY KEY (UsuarioId, RolId)
);
