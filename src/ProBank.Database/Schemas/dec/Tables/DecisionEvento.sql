-- DEC-06: trazabilidad de una decisión: registro, ediciones, envío, firmas, aprobadores y notificados agregados o quitados,
-- anulación, cada consulta y cada exportación. Solo recibe inserciones (como aud.Auditoria).
CREATE TABLE [dec].[DecisionEvento]
(
    EventoId   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DecisionEvento PRIMARY KEY,
    DecisionId INT NOT NULL CONSTRAINT FK_DecEvento_Decision REFERENCES dec.Decision (DecisionId),
    Fecha      DATETIME2(0) NOT NULL CONSTRAINT DF_DecEvento_Fecha DEFAULT (SYSDATETIME()),
    UsuarioId  INT NOT NULL CONSTRAINT FK_DecEvento_Usuario REFERENCES seg.Usuario (UsuarioId),
    Evento     NVARCHAR(40)  NOT NULL,
    Detalle    NVARCHAR(500) NULL,
    Ip         VARCHAR(45)   NULL
);
GO
CREATE INDEX IX_DecisionEvento_Decision ON dec.DecisionEvento (DecisionId, EventoId);
