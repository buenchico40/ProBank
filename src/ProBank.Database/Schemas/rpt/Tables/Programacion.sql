CREATE TABLE [rpt].[Programacion]
(
ProgramacionId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Programacion PRIMARY KEY,
    ConsultaId       INT NOT NULL CONSTRAINT FK_Programacion_Consulta REFERENCES rpt.Consulta (ConsultaId),
    ConexionId       INT NOT NULL CONSTRAINT FK_Programacion_Conexion REFERENCES rpt.Conexion (ConexionId),
    Frecuencia       VARCHAR(10) NOT NULL CONSTRAINT CK_Programacion_Frec CHECK (Frecuencia IN ('Diaria','Semanal','Mensual')),
    Hora             TIME(0) NOT NULL,
    Dias             VARCHAR(20) NULL,          -- semanal: 1,3,5 (DayOfWeek)
    DiaDelMes        TINYINT NULL,              -- mensual: 0 o NULL = último día
    Formato          VARCHAR(5) NOT NULL,
    ParametrosJson   NVARCHAR(2000) NOT NULL,   -- valores fijos o variables por parámetro
    ResponsableId    INT NOT NULL,
    ProximaEjecucion DATETIME2(0) NOT NULL,
    UltimoResultado  VARCHAR(100) NULL,
    Estado           VARCHAR(25) NOT NULL CONSTRAINT CK_Programacion_Estado CHECK (Estado IN ('Activa','Pausada','PendienteAprobacion'))
);
