-- Ajuste o movimiento de presupuesto (MOV-0001): Reasignación (de una partida a otra), Incremento (entra a una partida) o
-- Reducción (sale de una partida). Solo los aprobados cambian el presupuesto (Ajustes de com.vw_Partida).
CREATE TABLE [com].[Movimiento]
(
    MovimientoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ComMovimiento PRIMARY KEY,
    Token            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ComMov_Token DEFAULT (NEWID()) CONSTRAINT UQ_ComMov_Token UNIQUE,
    Codigo           AS ('MOV-' + RIGHT('0000' + CAST(MovimientoId AS VARCHAR(10)), 4)) PERSISTED,
    Fecha            DATE          NOT NULL,
    Tipo             NVARCHAR(20)  NOT NULL CONSTRAINT CK_ComMov_Tipo CHECK (Tipo IN (N'Reasignación', N'Incremento', N'Reducción')),
    PartidaOrigenId  INT NULL CONSTRAINT FK_ComMov_Origen REFERENCES com.Partida (PartidaId),
    PartidaDestinoId INT NULL CONSTRAINT FK_ComMov_Destino REFERENCES com.Partida (PartidaId),
    Monto            DECIMAL(18,2) NOT NULL CONSTRAINT CK_ComMov_Monto CHECK (Monto > 0),
    Motivo           NVARCHAR(500) NOT NULL,
    Estado           VARCHAR(20)   NOT NULL CONSTRAINT DF_ComMov_Estado DEFAULT ('Pendiente')
                     CONSTRAINT CK_ComMov_Estado CHECK (Estado IN ('Pendiente', 'Aprobada', 'Rechazada', 'Anulada')),
    RegistradoPorId  INT           NOT NULL CONSTRAINT FK_ComMov_Registrador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro    DATETIME2(0)  NOT NULL CONSTRAINT DF_ComMov_Registro DEFAULT (SYSDATETIME()),
    ResueltoPorId    INT           NULL CONSTRAINT FK_ComMov_Resolutor REFERENCES seg.Usuario (UsuarioId),
    FechaResolucion  DATETIME2(0)  NULL,
    Comentario       NVARCHAR(500) NULL,
    -- Reasignación: origen y destino distintos; Incremento: solo destino; Reducción: solo origen
    CONSTRAINT CK_ComMov_Partidas CHECK (
        (Tipo = N'Reasignación' AND PartidaOrigenId IS NOT NULL AND PartidaDestinoId IS NOT NULL AND PartidaOrigenId <> PartidaDestinoId)
        OR (Tipo = N'Incremento' AND PartidaOrigenId IS NULL AND PartidaDestinoId IS NOT NULL)
        OR (Tipo = N'Reducción' AND PartidaOrigenId IS NOT NULL AND PartidaDestinoId IS NULL))
);
