CREATE TABLE [rpt].[Ejecucion]
(
EjecucionId         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ejecucion PRIMARY KEY,
    Codigo              AS ('EJE-' + RIGHT('000000' + CAST(EjecucionId AS VARCHAR(20)), 6)) PERSISTED,
    ConsultaId          INT NOT NULL CONSTRAINT FK_Ejecucion_Consulta REFERENCES rpt.Consulta (ConsultaId),
    ConexionId          INT NOT NULL CONSTRAINT FK_Ejecucion_Conexion REFERENCES rpt.Conexion (ConexionId),
    SolicitanteId       INT NOT NULL,
    ParametrosResueltos NVARCHAR(2000) NOT NULL,
    Filas               INT NULL,
    DuracionMs          INT NULL,
    Estado              VARCHAR(20) NOT NULL,
    Fecha               DATETIME2(0) NOT NULL CONSTRAINT DF_Ejecucion_Fecha DEFAULT (SYSDATETIME())
);
