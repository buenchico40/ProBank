-- SEG-03: acciones posibles de cada opción. Tipo 'Accion' (ver, crear, editar, eliminar, exportar, aprobar)
-- o 'Funcion' (permiso propio de la opción, p. ej. revocar sesiones o restablecer la contraseña).
CREATE TABLE [seg].[OpcionAccion]
(
    Opcion  VARCHAR(60)   NOT NULL CONSTRAINT FK_OpcionAccion_Opcion REFERENCES seg.Opcion (Opcion),
    Accion  VARCHAR(60)   NOT NULL,
    Nombre  NVARCHAR(100) NOT NULL,
    Tipo    VARCHAR(10)   NOT NULL CONSTRAINT CK_OpcionAccion_Tipo CHECK (Tipo IN ('Accion', 'Funcion')),
    Orden   INT           NOT NULL,
    CONSTRAINT PK_OpcionAccion PRIMARY KEY (Opcion, Accion)
);
