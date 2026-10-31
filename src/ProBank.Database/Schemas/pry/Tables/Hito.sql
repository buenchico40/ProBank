-- Hitos opcionales del ítem; agrupan tareas en el cronograma. Las fechas son la planificación cuando aún no hay tareas.
CREATE TABLE [pry].[Hito]
(
HitoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Hito PRIMARY KEY,
    ItemId INT NOT NULL CONSTRAINT FK_Hito_Item REFERENCES pry.Item (ItemId),
    Nombre NVARCHAR(150) NOT NULL,
    Orden  INT NOT NULL,
    FechaInicio DATE NULL,
    FechaFin    DATE NULL,
    CONSTRAINT CK_Hito_Fechas CHECK (FechaFin IS NULL OR FechaInicio IS NULL OR FechaFin >= FechaInicio)
);
