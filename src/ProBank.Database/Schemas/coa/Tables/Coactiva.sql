-- Coactiva: nace con la orden de bloqueo del oficio que la trae por primera vez (OficioId).
-- COA-03: ProductosPermitidos = tipos de producto separados por coma; NULL = todos.
CREATE TABLE [coa].[Coactiva]
(
    CoactivaId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Coactiva PRIMARY KEY,
    Token               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Coactiva_Token DEFAULT (NEWID()) CONSTRAINT UQ_Coactiva_Token UNIQUE,
    OficioId            INT NOT NULL CONSTRAINT FK_Coactiva_Oficio REFERENCES coa.Oficio (OficioId),
    EntidadId           INT NOT NULL CONSTRAINT FK_Coactiva_Entidad REFERENCES coa.Entidad (EntidadId),
    ClienteId           INT NOT NULL CONSTRAINT FK_Coactiva_Cliente REFERENCES coa.Cliente (ClienteId),
    NumeroProceso       VARCHAR(50)   NOT NULL,
    Monto               DECIMAL(18,2) NOT NULL CONSTRAINT CK_Coactiva_Monto CHECK (Monto > 0),
    ProductosPermitidos VARCHAR(200)  NULL,
    Observacion         NVARCHAR(500) NULL,
    Anulada             BIT           NOT NULL CONSTRAINT DF_Coactiva_Anulada DEFAULT (0),
    CONSTRAINT UQ_Coactiva_Proceso UNIQUE (EntidadId, NumeroProceso)
);
