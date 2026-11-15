-- DEC-02: aprobadores de un tipo de decisión. Orden: turno en los flujos en secuencia.
CREATE TABLE [dec].[TipoAprobador]
(
    TipoId     INT NOT NULL CONSTRAINT FK_TipoAprobador_Tipo REFERENCES dec.TipoDecision (TipoId),
    UsuarioId  INT NOT NULL CONSTRAINT FK_TipoAprobador_Usuario REFERENCES seg.Usuario (UsuarioId),
    Orden      INT NOT NULL CONSTRAINT CK_TipoAprobador_Orden CHECK (Orden BETWEEN 1 AND 99),
    CONSTRAINT PK_TipoAprobador PRIMARY KEY (TipoId, UsuarioId)
);
