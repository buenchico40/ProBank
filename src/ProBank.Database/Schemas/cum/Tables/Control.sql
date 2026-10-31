CREATE TABLE [cum].[Control]
(
ControlId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Control PRIMARY KEY,
    Codigo        VARCHAR(20) NOT NULL CONSTRAINT UQ_Control UNIQUE,
    Normativa     NVARCHAR(150) NOT NULL,
    Descripcion   NVARCHAR(500) NOT NULL,
    ResponsableId INT NOT NULL,
    ProximaEvaluacion DATE NOT NULL
);
