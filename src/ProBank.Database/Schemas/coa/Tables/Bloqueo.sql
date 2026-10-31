-- Bloqueo de fondos en un producto del cliente para cubrir una coactiva. Lo levantado y transferido sale de coa.Movimiento.
CREATE TABLE [coa].[Bloqueo]
(
    BloqueoId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bloqueo PRIMARY KEY,
    Codigo      AS ('BLQ-' + RIGHT('0000' + CAST(BloqueoId AS VARCHAR(10)), 4)) PERSISTED,
    CoactivaId  INT NOT NULL CONSTRAINT FK_Bloqueo_Coactiva REFERENCES coa.Coactiva (CoactivaId),
    ProductoId  INT NOT NULL CONSTRAINT FK_Bloqueo_Producto REFERENCES coa.Producto (ProductoId),
    Monto       DECIMAL(18,2) NOT NULL CONSTRAINT CK_Bloqueo_Monto CHECK (Monto > 0),
    Fecha       DATE          NOT NULL,
    UsuarioId   INT           NOT NULL CONSTRAINT FK_Bloqueo_Usuario REFERENCES seg.Usuario (UsuarioId)
);
