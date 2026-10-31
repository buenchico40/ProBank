CREATE TABLE [com].[Solicitud]
(
SolicitudId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Solicitud PRIMARY KEY,
    Codigo        VARCHAR(20) NOT NULL CONSTRAINT UQ_Solicitud UNIQUE,
    Descripcion   NVARCHAR(300) NOT NULL,
    PartidaId     INT NULL CONSTRAINT FK_Solicitud_Partida REFERENCES com.Partida (PartidaId),
    Monto         DECIMAL(18,2) NOT NULL,
    Presupuestado BIT NOT NULL,
    SolicitanteId INT NOT NULL,
    Estado        VARCHAR(30) NOT NULL,
    CONSTRAINT CK_Solicitud_Partida CHECK (Presupuestado = 0 OR PartidaId IS NOT NULL)
);
