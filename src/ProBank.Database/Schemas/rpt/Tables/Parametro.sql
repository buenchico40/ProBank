CREATE TABLE [rpt].[Parametro]
(
ParametroId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Parametro PRIMARY KEY,
    ConsultaId   INT NOT NULL CONSTRAINT FK_Parametro_Consulta REFERENCES rpt.Consulta (ConsultaId),
    Nombre       SYSNAME NOT NULL,
    Etiqueta     NVARCHAR(100) NOT NULL,
    Tipo         VARCHAR(10) NOT NULL CONSTRAINT CK_Parametro_Tipo CHECK (Tipo IN ('Texto','Numero','Fecha','Lista')),
    Obligatorio  BIT NOT NULL,
    ValorDefecto NVARCHAR(200) NULL,   -- fijo o variable (Hoy, Fin de mes, Inicio del mes anterior, Año actual...)
    Orden        INT NOT NULL
);
