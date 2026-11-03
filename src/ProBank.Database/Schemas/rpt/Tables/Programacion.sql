-- SQL-07: envío programado de una consulta (PRG-0001), diario, semanal o mensual a una hora. Las variables de sus parámetros
-- se resuelven en cada corrida. Solo se programan consultas publicadas sin aprobadores (la corrida no espera a nadie).
CREATE TABLE [rpt].[Programacion]
(
    ProgramacionId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Programacion PRIMARY KEY,
    Codigo           AS ('PRG-' + RIGHT('0000' + CAST(ProgramacionId AS VARCHAR(10)), 4)) PERSISTED,
    ConsultaId       INT NOT NULL CONSTRAINT FK_Programacion_Consulta REFERENCES rpt.Consulta (ConsultaId),
    ConexionId       INT NOT NULL CONSTRAINT FK_Programacion_Conexion REFERENCES rpt.Conexion (ConexionId),
    Frecuencia       VARCHAR(10) NOT NULL CONSTRAINT CK_Programacion_Frec CHECK (Frecuencia IN ('Diaria','Semanal','Mensual')),
    Hora             TIME(0) NOT NULL,
    Dias             VARCHAR(20) NULL,          -- semanal: 1,3,5 (DayOfWeek)
    DiaDelMes        TINYINT NULL,              -- mensual: 0 o NULL = último día
    Formato          VARCHAR(5) NOT NULL CONSTRAINT CK_Programacion_Formato CHECK (Formato IN ('xlsx','csv','pdf','json','txt')),
    ParametrosJson   NVARCHAR(2000) NOT NULL,   -- valores fijos o variables por parámetro
    ResponsableId    INT NOT NULL CONSTRAINT FK_Programacion_Responsable REFERENCES seg.Usuario (UsuarioId),
    ProximaEjecucion DATETIME2(0) NOT NULL,
    UltimaEjecucion  DATETIME2(0) NULL,
    UltimoResultado  VARCHAR(100) NULL,
    Estado           VARCHAR(25) NOT NULL CONSTRAINT CK_Programacion_Estado CHECK (Estado IN ('Activa','Pausada'))
);
