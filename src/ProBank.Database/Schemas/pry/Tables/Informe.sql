-- Informe de seguimiento (semanal o mensual): al generarlo se guarda una copia con la bitácora y los acuerdos del periodo.
CREATE TABLE [pry].[Informe]
(
    InformeId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Informe PRIMARY KEY,
    Codigo         AS ('INF-' + RIGHT('0000' + CAST(InformeId AS VARCHAR(10)), 4)) PERSISTED,
    Tipo           VARCHAR(10)   NOT NULL CONSTRAINT CK_Informe_Tipo CHECK (Tipo IN ('Semanal','Mensual')),
    Desde          DATE          NOT NULL,
    Hasta          DATE          NOT NULL,
    Incluye        NVARCHAR(100) NOT NULL CONSTRAINT DF_Informe_Incluye DEFAULT (N'Bitácora y acuerdos'),
    Estado         VARCHAR(20)   NOT NULL CONSTRAINT DF_Informe_Estado DEFAULT ('Borrador') CONSTRAINT CK_Informe_Estado CHECK (Estado IN ('Borrador','Completado')),
    ContenidoHtml  NVARCHAR(MAX) NULL,
    CreadoPorId    INT           NOT NULL CONSTRAINT FK_Informe_Creador REFERENCES seg.Usuario (UsuarioId),
    GeneradoPorId  INT           NULL CONSTRAINT FK_Informe_Generador REFERENCES seg.Usuario (UsuarioId),
    FechaGenerado  DATETIME2(0)  NULL,
    CONSTRAINT CK_Informe_Periodo CHECK (Hasta >= Desde)
);
