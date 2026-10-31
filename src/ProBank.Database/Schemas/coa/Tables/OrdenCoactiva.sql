-- COA-01: orden judicial de un oficio sobre una coactiva. Bloqueo = la coactiva nueva que trae el oficio;
-- levantamiento y transferencia = sobre una coactiva ya bloqueada. MontoOrdenado NULL = total.
CREATE TABLE [coa].[OrdenCoactiva]
(
    OrdenId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenCoactiva PRIMARY KEY,
    OficioId      INT NOT NULL CONSTRAINT FK_Orden_Oficio REFERENCES coa.Oficio (OficioId),
    CoactivaId    INT NOT NULL CONSTRAINT FK_Orden_Coactiva REFERENCES coa.Coactiva (CoactivaId),
    Tipo          VARCHAR(20)   NOT NULL CONSTRAINT CK_Orden_Tipo CHECK (Tipo IN ('Bloqueo', 'Levantamiento', 'Transferencia')),
    MontoOrdenado DECIMAL(18,2) NULL CONSTRAINT CK_Orden_Monto CHECK (MontoOrdenado IS NULL OR MontoOrdenado > 0),
    Observacion   NVARCHAR(500) NULL,
    CONSTRAINT UQ_Orden UNIQUE (OficioId, CoactivaId, Tipo)
);
