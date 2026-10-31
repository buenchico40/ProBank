CREATE TABLE [com].[Renovacion]
(
RenovacionId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Renovacion PRIMARY KEY,
    Servicio      NVARCHAR(150) NOT NULL,
    Proveedor     NVARCHAR(150) NOT NULL,
    Vence         DATE NOT NULL,
    DiasAviso     INT NOT NULL,
    PartidaId     INT NULL CONSTRAINT FK_Renovacion_Partida REFERENCES com.Partida (PartidaId),
    Presupuestado BIT NOT NULL,
    Estado        VARCHAR(20) NOT NULL
);
