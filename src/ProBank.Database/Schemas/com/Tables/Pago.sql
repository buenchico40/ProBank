CREATE TABLE [com].[Pago]
(
PagoId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pago PRIMARY KEY,
    PartidaId INT NOT NULL CONSTRAINT FK_Pago_Partida REFERENCES com.Partida (PartidaId),
    Proveedor NVARCHAR(150) NOT NULL,
    Fecha     DATE NOT NULL,
    Valor     DECIMAL(18,2) NOT NULL,
    Estado    VARCHAR(20) NOT NULL
);
