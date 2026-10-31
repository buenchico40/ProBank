CREATE TABLE [rpt].[EjecucionAprobador]
(
EjecucionId BIGINT NOT NULL CONSTRAINT FK_EA_Ejecucion REFERENCES rpt.Ejecucion (EjecucionId),
    UsuarioId   INT NOT NULL,
    Decision    VARCHAR(10) NULL,
    Comentario  NVARCHAR(1000) NULL,
    Fecha       DATETIME2(0) NULL,
    CONSTRAINT PK_EjecucionAprobador PRIMARY KEY (EjecucionId, UsuarioId)
);
