CREATE TABLE [seg].[Permiso]
(
RolId   INT NOT NULL CONSTRAINT FK_Permiso_Rol REFERENCES seg.Rol (RolId),
    Opcion  VARCHAR(60) NOT NULL,
    Accion  VARCHAR(60) NOT NULL,
    CONSTRAINT PK_Permiso PRIMARY KEY (RolId, Opcion, Accion),
    CONSTRAINT FK_Permiso_OpcionAccion FOREIGN KEY (Opcion, Accion) REFERENCES seg.OpcionAccion (Opcion, Accion)
);
