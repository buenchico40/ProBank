-- COM-04: cuánto de un pago se aplica a cada partida (debe sumar el monto del pago para marcarlo Pagado).
CREATE TABLE [com].[PagoDistribucion]
(
    PagoId    INT NOT NULL CONSTRAINT FK_PagoDist_Pago REFERENCES com.Pago (PagoId),
    PartidaId INT NOT NULL CONSTRAINT FK_PagoDist_Partida REFERENCES com.Partida (PartidaId),
    Monto     DECIMAL(18,2) NOT NULL CONSTRAINT CK_PagoDist_Monto CHECK (Monto > 0),
    CONSTRAINT PK_PagoDistribucion PRIMARY KEY (PagoId, PartidaId),
    INDEX IX_PagoDist_Partida (PartidaId)
);
