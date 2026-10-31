CREATE TABLE [ver].[Version]
(
VersionId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Version PRIMARY KEY,
    ComponenteId   INT NOT NULL CONSTRAINT FK_Version_Componente REFERENCES ver.Componente (ComponenteId),
    Mayor INT NOT NULL, Menor INT NOT NULL, Parche INT NOT NULL,
    FechaLiberacion DATE NULL,
    NotasHtml      NVARCHAR(MAX) NULL,
    Estado         VARCHAR(20) NOT NULL CONSTRAINT CK_Version_Estado CHECK (Estado IN ('Planificada','EnPruebas','Aprobada','Desplegada','Revertida')),
    CONSTRAINT UQ_Version UNIQUE (ComponenteId, Mayor, Menor, Parche)
);
