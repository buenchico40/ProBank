-- MKT-02: comunicado del Panel con contenido estructurado (resumen, secciones con el editor enriquecido y contacto),
-- tipo, vigencia, audiencia y destacado. Mismos estados que la promoción (mkt.fn_EstadoVigente).
CREATE TABLE [mkt].[Comunicado]
(
    ComunicadoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comunicado PRIMARY KEY,
    Token            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Comunicado_Token DEFAULT (NEWID()) CONSTRAINT UQ_Comunicado_Token UNIQUE,
    Codigo           AS ('COM-' + RIGHT('000' + CAST(ComunicadoId AS VARCHAR(10)), 3)) PERSISTED,
    Titulo           NVARCHAR(200) NOT NULL,
    Tipo             NVARCHAR(20)  NOT NULL CONSTRAINT CK_Comunicado_Tipo CHECK (Tipo IN (N'Aviso', N'Noticia', N'Mantenimiento', N'Campaña')),
    Resumen          NVARCHAR(300) NOT NULL,
    ContenidoHtml    NVARCHAR(MAX) NULL,
    Contacto         NVARCHAR(150) NULL,
    Desde            DATE          NULL,
    Hasta            DATE          NULL,
    Audiencia        NVARCHAR(60)  NOT NULL CONSTRAINT DF_Comunicado_Audiencia DEFAULT (N'Todos'),
    Destacado        BIT           NOT NULL CONSTRAINT DF_Comunicado_Destacado DEFAULT (0),
    Estado           VARCHAR(20)   NOT NULL CONSTRAINT DF_Comunicado_Estado DEFAULT ('Borrador')
                     CONSTRAINT CK_Comunicado_Estado CHECK (Estado IN ('Borrador', 'Publicado', 'Retirado')),
    CreadoPorId      INT           NOT NULL CONSTRAINT FK_Comunicado_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro    DATETIME2(0)  NOT NULL CONSTRAINT DF_Comunicado_Registro DEFAULT (SYSDATETIME()),
    PublicadoPorId   INT           NULL CONSTRAINT FK_Comunicado_Publicador REFERENCES seg.Usuario (UsuarioId),
    FechaPublicacion DATETIME2(0)  NULL,
    CONSTRAINT CK_Comunicado_Vigencia CHECK (Hasta IS NULL OR Desde IS NULL OR Hasta >= Desde)
);
