-- Cada operación ejecutada sobre un bloqueo, con el oficio (orden) que la ordena. OrdenId NULL = levantamiento por anulación.
CREATE TABLE [coa].[Movimiento]
(
    MovimientoId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CoaMovimiento PRIMARY KEY,
    BloqueoId     INT NOT NULL CONSTRAINT FK_Movimiento_Bloqueo REFERENCES coa.Bloqueo (BloqueoId),
    OrdenId       INT NULL CONSTRAINT FK_Movimiento_Orden REFERENCES coa.OrdenCoactiva (OrdenId),
    Tipo          VARCHAR(20)   NOT NULL CONSTRAINT CK_Movimiento_Tipo CHECK (Tipo IN ('Bloqueo', 'Levantamiento', 'Transferencia')),
    Monto         DECIMAL(18,2) NOT NULL CONSTRAINT CK_Movimiento_Monto CHECK (Monto > 0),
    Fecha         DATE          NOT NULL,
    FormaPago     VARCHAR(40)   NULL,
    CuentaDestino NVARCHAR(150) NULL,
    Comprobante   NVARCHAR(60)  NULL,
    Detalle       NVARCHAR(500) NULL,
    UsuarioId     INT           NOT NULL CONSTRAINT FK_CoaMovimiento_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_CoaMovimiento_Registro DEFAULT (SYSDATETIME())
);
