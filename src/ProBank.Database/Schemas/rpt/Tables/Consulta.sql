CREATE TABLE [rpt].[Consulta]
(
ConsultaId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Consulta PRIMARY KEY,
    Codigo          VARCHAR(20) NOT NULL CONSTRAINT UQ_Consulta UNIQUE,
    Nombre          NVARCHAR(150) NOT NULL,
    Procedimiento   NVARCHAR(257) NOT NULL CONSTRAINT CK_Consulta_Sp CHECK (Procedimiento LIKE '%_._%' AND Procedimiento NOT LIKE '%[ ;''-]%'),
    TiempoMaximoSeg INT NOT NULL CONSTRAINT DF_Consulta_Tiempo DEFAULT (30),
    LimiteFilas     INT NOT NULL CONSTRAINT DF_Consulta_Limite DEFAULT (10000),
    Estado          VARCHAR(20) NOT NULL CONSTRAINT CK_Consulta_Estado CHECK (Estado IN ('Borrador','Publicada'))
);
