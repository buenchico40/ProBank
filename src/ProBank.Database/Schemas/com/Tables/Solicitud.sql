-- Solicitud de compra (SC-0001). COM-02: presupuestada (imputa a una partida) o no presupuestada (no descuenta de ninguna).
-- Aprobada queda como comprometido de su partida hasta que se paga (com.vw_Solicitud y com.vw_Partida).
CREATE TABLE [com].[Solicitud]
(
    SolicitudId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Solicitud PRIMARY KEY,
    Token           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Solicitud_Token DEFAULT (NEWID()) CONSTRAINT UQ_Solicitud_Token UNIQUE,
    Codigo          AS ('SC-' + RIGHT('0000' + CAST(SolicitudId AS VARCHAR(10)), 4)) PERSISTED,
    Descripcion     NVARCHAR(300) NOT NULL,
    Area            NVARCHAR(60)  NOT NULL,
    Proveedor       NVARCHAR(150) NULL,
    Presupuestado   BIT           NOT NULL,
    PartidaId       INT NULL CONSTRAINT FK_Solicitud_Partida REFERENCES com.Partida (PartidaId),
    Monto           DECIMAL(18,2) NOT NULL CONSTRAINT CK_Solicitud_Monto CHECK (Monto > 0),
    RenovacionId    INT NULL CONSTRAINT FK_Solicitud_Renovacion REFERENCES com.Renovacion (RenovacionId),
    Aprobacion      VARCHAR(20)   NOT NULL CONSTRAINT DF_Solicitud_Aprobacion DEFAULT ('Pendiente')
                    CONSTRAINT CK_Solicitud_Aprobacion CHECK (Aprobacion IN ('Pendiente', 'Aprobada', 'Rechazada')),
    Anulada         BIT           NOT NULL CONSTRAINT DF_Solicitud_Anulada DEFAULT (0),
    SolicitanteId   INT           NOT NULL CONSTRAINT FK_Solicitud_Solicitante REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Solicitud_Registro DEFAULT (SYSDATETIME()),
    AprobadoPorId   INT           NULL CONSTRAINT FK_Solicitud_Aprobador REFERENCES seg.Usuario (UsuarioId),
    FechaAprobacion DATETIME2(0)  NULL,
    Comentario      NVARCHAR(500) NULL,
    CONSTRAINT CK_Solicitud_Partida CHECK ((Presupuestado = 1 AND PartidaId IS NOT NULL) OR (Presupuestado = 0 AND PartidaId IS NULL))
);
