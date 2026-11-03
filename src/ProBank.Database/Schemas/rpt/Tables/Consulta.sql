-- SQL-01: catálogo de consultas (QRY-001). Cada una ejecuta un procedimiento almacenado de dos partes (esquema.nombre); nunca
-- texto SQL. Sus parámetros y las columnas del resultado se leen de la base (sys.parameters y sp_describe_first_result_set).
-- Solo una consulta Publicada se ejecuta; cambiar su procedimiento o sus bases la devuelve a Borrador.
CREATE TABLE [rpt].[Consulta]
(
    ConsultaId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Consulta PRIMARY KEY,
    Codigo          VARCHAR(20)   NOT NULL CONSTRAINT UQ_Consulta UNIQUE,
    Nombre          NVARCHAR(150) NOT NULL,
    Descripcion     NVARCHAR(500) NULL,
    Procedimiento   NVARCHAR(257) NOT NULL CONSTRAINT CK_Consulta_Sp CHECK (Procedimiento LIKE '%_._%' AND Procedimiento NOT LIKE '%[^A-Za-z0-9_.]%'),
    TiempoMaximoSeg INT           NOT NULL CONSTRAINT DF_Consulta_Tiempo DEFAULT (30) CONSTRAINT CK_Consulta_Tiempo CHECK (TiempoMaximoSeg BETWEEN 5 AND 600),
    LimiteFilas     INT           NOT NULL CONSTRAINT DF_Consulta_Limite DEFAULT (10000) CONSTRAINT CK_Consulta_Limite CHECK (LimiteFilas BETWEEN 1 AND 100000),
    Estado          VARCHAR(20)   NOT NULL CONSTRAINT DF_Consulta_Estado DEFAULT ('Borrador') CONSTRAINT CK_Consulta_Estado CHECK (Estado IN ('Borrador', 'Publicada')),
    -- Columnas del primer resultado ([{"nombre":"Saldo","tipo":"decimal(18,2)"}]) y cuándo se leyó la definición
    ColumnasJson    NVARCHAR(MAX) NULL,
    DefinicionLeida DATETIME2(0)  NULL,
    CreadoPorId     INT           NOT NULL CONSTRAINT FK_Consulta_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Consulta_Registro DEFAULT (SYSDATETIME())
);
