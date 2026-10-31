CREATE TABLE [rpt].[EjecucionEvento]
(
EventoId    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EjecucionEvento PRIMARY KEY,
    EjecucionId BIGINT NOT NULL CONSTRAINT FK_Evento_Ejecucion REFERENCES rpt.Ejecucion (EjecucionId),
    Fecha       DATETIME2(3) NOT NULL CONSTRAINT DF_Evento_Fecha DEFAULT (SYSDATETIME()),
    UsuarioId   INT NULL,
    Evento      VARCHAR(30) NOT NULL,
    Detalle     NVARCHAR(2000) NULL,
    Filas       INT NULL,
    DuracionMs  INT NULL,
    Resultado   VARCHAR(30) NOT NULL,
    Ip          VARCHAR(45) NULL
);
