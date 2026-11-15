-- DEC-05: personas notificadas de una decisión: la ven (solo lectura) y cada consulta queda en su trazabilidad.
CREATE TABLE [dec].[DecisionNotificado]
(
    DecisionId    INT NOT NULL CONSTRAINT FK_DecNotificado_Decision REFERENCES dec.Decision (DecisionId),
    UsuarioId     INT NOT NULL CONSTRAINT FK_DecNotificado_Usuario REFERENCES seg.Usuario (UsuarioId),
    AgregadoPorId INT NOT NULL CONSTRAINT FK_DecNotificado_AgregadoPor REFERENCES seg.Usuario (UsuarioId),
    Fecha         DATETIME2(0) NOT NULL CONSTRAINT DF_DecNotificado_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_DecisionNotificado PRIMARY KEY (DecisionId, UsuarioId)
);
