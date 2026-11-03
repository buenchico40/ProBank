-- SQL-03: aprobadores de una ejecución (copiados de la consulta al solicitarla, sin el solicitante). Aprueban todos, en
-- cualquier orden; un rechazo (con motivo) la cierra.
CREATE TABLE [rpt].[EjecucionAprobador]
(
    EjecucionId BIGINT NOT NULL CONSTRAINT FK_EA_Ejecucion REFERENCES rpt.Ejecucion (EjecucionId),
    UsuarioId   INT NOT NULL CONSTRAINT FK_EA_Usuario REFERENCES seg.Usuario (UsuarioId),
    Decision    VARCHAR(10) NULL CONSTRAINT CK_EA_Decision CHECK (Decision IN ('Aprobada', 'Rechazada')),
    Comentario  NVARCHAR(1000) NULL,
    Fecha       DATETIME2(0) NULL,
    CONSTRAINT PK_EjecucionAprobador PRIMARY KEY (EjecucionId, UsuarioId)
);
