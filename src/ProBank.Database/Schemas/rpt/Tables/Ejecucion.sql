-- Una solicitud de ejecución (EJE-000001): con aprobadores queda PendienteAprobacion hasta que aprueben todos (SQL-03).
-- Estados: PendienteAprobacion, Aprobada, Rechazada, Ejecutada o Fallida. ParametrosJson guarda los valores ya resueltos
-- (lo que vieron los aprobadores); el resultado queda en el almacén (RutaResultado) para verlo y exportarlo sin reejecutar.
CREATE TABLE [rpt].[Ejecucion]
(
    EjecucionId         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ejecucion PRIMARY KEY,
    Codigo              AS ('EJE-' + RIGHT('000000' + CAST(EjecucionId AS VARCHAR(20)), 6)) PERSISTED,
    ConsultaId          INT NOT NULL CONSTRAINT FK_Ejecucion_Consulta REFERENCES rpt.Consulta (ConsultaId),
    ConexionId          INT NOT NULL CONSTRAINT FK_Ejecucion_Conexion REFERENCES rpt.Conexion (ConexionId),
    SolicitanteId       INT NOT NULL CONSTRAINT FK_Ejecucion_Solicitante REFERENCES seg.Usuario (UsuarioId),
    ParametrosResueltos NVARCHAR(2000) NOT NULL,
    ParametrosJson      NVARCHAR(2000) NOT NULL CONSTRAINT DF_Ejecucion_ParametrosJson DEFAULT (N'{}'),
    Programada          BIT NOT NULL CONSTRAINT DF_Ejecucion_Programada DEFAULT (0),
    Filas               INT NULL,
    DuracionMs          INT NULL,
    Truncado            BIT NOT NULL CONSTRAINT DF_Ejecucion_Truncado DEFAULT (0),
    RutaResultado       NVARCHAR(400) NULL,
    Mensaje             NVARCHAR(500) NULL,
    Estado              VARCHAR(20) NOT NULL CONSTRAINT CK_Ejecucion_Estado
                        CHECK (Estado IN ('PendienteAprobacion', 'Aprobada', 'Rechazada', 'Ejecutada', 'Fallida')),
    Fecha               DATETIME2(0) NOT NULL CONSTRAINT DF_Ejecucion_Fecha DEFAULT (SYSDATETIME())
);
