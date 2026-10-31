CREATE TABLE [com].[Movimiento]
(
MovimientoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ComMovimiento PRIMARY KEY,
    PartidaOrigenId  INT NULL CONSTRAINT FK_ComMov_Origen REFERENCES com.Partida (PartidaId),
    PartidaDestinoId INT NULL CONSTRAINT FK_ComMov_Destino REFERENCES com.Partida (PartidaId),
    Monto  DECIMAL(18,2) NOT NULL,
    Motivo NVARCHAR(500) NOT NULL,
    Estado VARCHAR(20) NOT NULL
);
