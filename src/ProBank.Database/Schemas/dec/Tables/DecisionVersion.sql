-- DEC-07: cada versión enviada del contenido de una decisión: la 1 al enviarla y una nueva cada vez que un aprobador la modifica
-- (con su motivo). Solo recibe inserciones.
CREATE TABLE [dec].[DecisionVersion]
(
    DecisionId    INT           NOT NULL CONSTRAINT FK_DecVersion_Decision REFERENCES dec.Decision (DecisionId),
    Version       INT           NOT NULL,
    Titulo        NVARCHAR(200) NOT NULL,
    Descripcion   NVARCHAR(MAX) NOT NULL,
    Area          NVARCHAR(100) NOT NULL,
    Fecha         DATE          NOT NULL,
    Huella        VARBINARY(32) NOT NULL,
    Motivo        NVARCHAR(500) NULL,
    UsuarioId     INT           NOT NULL CONSTRAINT FK_DecVersion_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_DecVersion_Registro DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_DecisionVersion PRIMARY KEY (DecisionId, Version)
);
