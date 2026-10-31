-- Oficio judicial: trae varias coactivas de varios clientes y entidades, con órdenes mezcladas (COA-02).
-- Su estado (Pendiente, En curso, Atendido) se calcula en coa.vw_Oficio; aquí solo se guarda si está anulado.
CREATE TABLE [coa].[Oficio]
(
    OficioId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Oficio PRIMARY KEY,
    Token           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Oficio_Token DEFAULT (NEWID()) CONSTRAINT UQ_Oficio_Token UNIQUE,
    Numero          VARCHAR(30)   NOT NULL CONSTRAINT UQ_Oficio_Numero UNIQUE,
    Recibido        DATE          NOT NULL,
    RemitidoPor     NVARCHAR(150) NOT NULL,
    ContenidoHtml   NVARCHAR(MAX) NULL,
    BlobUri         NVARCHAR(400) NULL,
    Anulado         BIT           NOT NULL CONSTRAINT DF_Oficio_Anulado DEFAULT (0),
    RegistradoPorId INT           NOT NULL CONSTRAINT FK_Oficio_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Oficio_Registro DEFAULT (SYSDATETIME())
);
