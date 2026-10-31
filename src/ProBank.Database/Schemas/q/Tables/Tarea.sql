CREATE TABLE [q].[Tarea]
(
TareaId     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QTarea PRIMARY KEY,
    Tipo        VARCHAR(60) NOT NULL,
    Payload     NVARCHAR(MAX) NOT NULL,
    Estado      VARCHAR(20) NOT NULL CONSTRAINT DF_QTarea_Estado DEFAULT ('Pendiente'),
    Intentos    INT NOT NULL CONSTRAINT DF_QTarea_Intentos DEFAULT (0),
    Creada      DATETIME2(0) NOT NULL CONSTRAINT DF_QTarea_Creada DEFAULT (SYSDATETIME()),
    TomadaPor   VARCHAR(100) NULL,
    Terminada   DATETIME2(0) NULL,
    Error       NVARCHAR(2000) NULL
);
