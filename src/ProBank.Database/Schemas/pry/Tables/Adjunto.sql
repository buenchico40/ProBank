-- Documentos adjuntos de un ítem (proyecto, mejora o bug) o de una tarea. El archivo vive en Blob Storage (contenedor adjuntos) en Ruta.
CREATE TABLE [pry].[Adjunto]
(
    AdjuntoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Adjunto PRIMARY KEY,
    Token         UNIQUEIDENTIFIER NOT NULL CONSTRAINT UQ_Adjunto_Token UNIQUE,
    ItemId        INT NULL CONSTRAINT FK_Adjunto_Item REFERENCES pry.Item (ItemId),
    TareaId       INT NULL CONSTRAINT FK_Adjunto_Tarea REFERENCES pry.Tarea (TareaId),
    Nombre        NVARCHAR(200) NOT NULL,
    Descripcion   NVARCHAR(300) NOT NULL,
    TipoContenido VARCHAR(100)  NOT NULL,
    Tamano        BIGINT        NOT NULL CONSTRAINT CK_Adjunto_Tamano CHECK (Tamano > 0),
    Ruta          VARCHAR(300)  NOT NULL,
    SubidoPorId   INT NOT NULL CONSTRAINT FK_Adjunto_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Adjunto_Registro DEFAULT (SYSDATETIME()),
    -- Pertenece a un ítem o a una tarea, nunca a ambos
    CONSTRAINT CK_Adjunto_Dueno CHECK ((ItemId IS NULL AND TareaId IS NOT NULL) OR (ItemId IS NOT NULL AND TareaId IS NULL)),
    INDEX IX_Adjunto_Item (ItemId),
    INDEX IX_Adjunto_Tarea (TareaId)
);
