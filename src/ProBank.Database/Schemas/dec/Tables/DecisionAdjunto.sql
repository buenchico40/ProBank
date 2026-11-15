-- DEC-08: archivos de respaldo de una decisión (en Blob Storage, contenedor adjuntos, ruta dec/<decisión>/<archivo>) con su huella
-- SHA-256 para identificarlos. Se agregan y quitan en borrador; desde el envío quedan fijos (no forman parte de la huella de la decisión).
CREATE TABLE [dec].[DecisionAdjunto]
(
    AdjuntoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DecisionAdjunto PRIMARY KEY,
    Token         UNIQUEIDENTIFIER NOT NULL CONSTRAINT UQ_DecisionAdjunto_Token UNIQUE,
    DecisionId    INT           NOT NULL CONSTRAINT FK_DecAdjunto_Decision REFERENCES dec.Decision (DecisionId),
    Nombre        NVARCHAR(200) NOT NULL,
    TipoContenido VARCHAR(150)  NOT NULL,
    Tamano        BIGINT        NOT NULL CONSTRAINT CK_DecAdjunto_Tamano CHECK (Tamano > 0),
    Hash          VARBINARY(32) NOT NULL,
    Ruta          VARCHAR(300)  NOT NULL,
    UsuarioId     INT           NOT NULL CONSTRAINT FK_DecAdjunto_Usuario REFERENCES seg.Usuario (UsuarioId),
    Fecha         DATETIME2(0)  NOT NULL CONSTRAINT DF_DecAdjunto_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_DecisionAdjunto_Nombre UNIQUE (DecisionId, Nombre)
);
