CREATE TABLE [com].[Partida]
(
PartidaId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Partida PRIMARY KEY,
    Anio       SMALLINT NOT NULL,
    Rubro      NVARCHAR(150) NOT NULL,
    Asignado   DECIMAL(18,2) NOT NULL,
    Estado     VARCHAR(20) NOT NULL CONSTRAINT CK_Partida_Estado CHECK (Estado IN ('Borrador','Activo','Cerrado')),
    CONSTRAINT UQ_Partida UNIQUE (Anio, Rubro)
);
