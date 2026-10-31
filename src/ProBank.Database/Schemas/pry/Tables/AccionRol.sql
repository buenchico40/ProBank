CREATE TABLE [pry].[AccionRol]
(
Accion VARCHAR(40) NOT NULL CONSTRAINT FK_AccionRol_Accion REFERENCES pry.Accion (Clave),
    RolId  INT NOT NULL CONSTRAINT FK_AccionRol_Rol REFERENCES seg.Rol (RolId),
    CONSTRAINT PK_AccionRol PRIMARY KEY (Accion, RolId)
);
