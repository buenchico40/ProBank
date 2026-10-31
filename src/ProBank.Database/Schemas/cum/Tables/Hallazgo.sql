CREATE TABLE [cum].[Hallazgo]
(
HallazgoId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Hallazgo PRIMARY KEY,
    Codigo        VARCHAR(20) NOT NULL CONSTRAINT UQ_Hallazgo UNIQUE,
    Riesgo        VARCHAR(10) NOT NULL,
    PlanAccion    NVARCHAR(1000) NOT NULL,
    FechaCompromiso DATE NOT NULL,
    Avance        TINYINT NOT NULL CONSTRAINT DF_Hallazgo_Avance DEFAULT (0),
    Estado        VARCHAR(20) NOT NULL
);
