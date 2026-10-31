CREATE TABLE [mkt].[Comunicado]
(
ComunicadoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comunicado PRIMARY KEY,
    Codigo       VARCHAR(20) NOT NULL CONSTRAINT UQ_Comunicado UNIQUE,
    Titulo       NVARCHAR(200) NOT NULL,
    Tipo         VARCHAR(30) NOT NULL,
    ContenidoHtml NVARCHAR(MAX) NOT NULL,
    Desde        DATE NOT NULL,
    Hasta        DATE NULL,
    Audiencia    VARCHAR(60) NOT NULL,
    Destacado    BIT NOT NULL CONSTRAINT DF_Comunicado_Destacado DEFAULT (0),
    Estado       VARCHAR(20) NOT NULL
);
