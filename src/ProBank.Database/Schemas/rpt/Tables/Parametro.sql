-- Parámetros de una consulta, detectados del procedimiento (sys.parameters) y configurados después: etiqueta, control,
-- si es obligatorio, valor por defecto fijo o variable (SQL-04: Hoy, Fin de mes, Inicio del mes anterior, Año actual…)
-- y, para una lista, sus opciones separadas por coma.
CREATE TABLE [rpt].[Parametro]
(
    ParametroId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Parametro PRIMARY KEY,
    ConsultaId   INT NOT NULL CONSTRAINT FK_Parametro_Consulta REFERENCES rpt.Consulta (ConsultaId),
    Nombre       SYSNAME NOT NULL,
    Etiqueta     NVARCHAR(100) NOT NULL,
    Tipo         VARCHAR(10) NOT NULL CONSTRAINT CK_Parametro_Tipo CHECK (Tipo IN ('Texto','Numero','Fecha','Lista')),
    TipoSql      VARCHAR(40) NULL,
    Obligatorio  BIT NOT NULL,
    ValorDefecto NVARCHAR(200) NULL,
    Opciones     NVARCHAR(1000) NULL,
    Orden        INT NOT NULL,
    CONSTRAINT UQ_Parametro UNIQUE (ConsultaId, Nombre)
);
