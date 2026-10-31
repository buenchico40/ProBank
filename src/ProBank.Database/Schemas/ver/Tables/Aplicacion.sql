CREATE TABLE [ver].[Aplicacion]
(
AplicacionId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Aplicacion PRIMARY KEY,
    Nombre        NVARCHAR(150) NOT NULL CONSTRAINT UQ_Aplicacion UNIQUE,
    ResponsableId INT NOT NULL
);
