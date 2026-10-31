CREATE TABLE [ver].[Componente]
(
ComponenteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Componente PRIMARY KEY,
    AplicacionId INT NOT NULL CONSTRAINT FK_Componente_App REFERENCES ver.Aplicacion (AplicacionId),
    Nombre       NVARCHAR(150) NOT NULL,
    Tipo         VARCHAR(20) NOT NULL CONSTRAINT CK_Componente_Tipo CHECK (Tipo IN ('Web','API','Servicio','BaseDatos','Libreria','Reporte')),
    Repositorio  NVARCHAR(400) NULL
);
