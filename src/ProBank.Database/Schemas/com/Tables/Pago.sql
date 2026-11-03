-- Pago (PAG-0001) de una solicitud aprobada. COM-04: al marcarse Pagado, su distribución (com.PagoDistribucion) descuenta
-- de cada partida. Las solicitudes no presupuestadas se pagan sin distribución (no descuentan de ninguna partida).
CREATE TABLE [com].[Pago]
(
    PagoId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pago PRIMARY KEY,
    Token           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Pago_Token DEFAULT (NEWID()) CONSTRAINT UQ_Pago_Token UNIQUE,
    Codigo          AS ('PAG-' + RIGHT('0000' + CAST(PagoId AS VARCHAR(10)), 4)) PERSISTED,
    SolicitudId     INT NOT NULL CONSTRAINT FK_Pago_Solicitud REFERENCES com.Solicitud (SolicitudId),
    Proveedor       NVARCHAR(150) NOT NULL,
    Fecha           DATE          NOT NULL,
    Monto           DECIMAL(18,2) NOT NULL CONSTRAINT CK_Pago_Monto CHECK (Monto > 0),
    Estado          VARCHAR(20)   NOT NULL CONSTRAINT DF_Pago_Estado DEFAULT ('Pendiente')
                    CONSTRAINT CK_Pago_Estado CHECK (Estado IN ('Pendiente', 'Pagado', 'Anulado')),
    RegistradoPorId INT           NOT NULL CONSTRAINT FK_Pago_Registrador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Pago_Registro DEFAULT (SYSDATETIME()),
    PagadoPorId     INT           NULL CONSTRAINT FK_Pago_Pagador REFERENCES seg.Usuario (UsuarioId),
    FechaPagado     DATETIME2(0)  NULL
);
