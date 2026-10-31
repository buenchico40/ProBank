CREATE TABLE [pry].[Tarea]
(
TareaId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tarea PRIMARY KEY,
    Codigo           AS ('TAR-' + RIGHT('0000' + CAST(TareaId AS VARCHAR(10)), 4)) PERSISTED,
    Token            UNIQUEIDENTIFIER NOT NULL CONSTRAINT UQ_Tarea_Token UNIQUE,
    ItemId           INT NOT NULL CONSTRAINT FK_Tarea_Item REFERENCES pry.Item (ItemId),
    HitoId           INT NULL CONSTRAINT FK_Tarea_Hito REFERENCES pry.Hito (HitoId),
    Titulo           NVARCHAR(200) NOT NULL,
    DescripcionHtml  NVARCHAR(MAX) NULL,
    Area             VARCHAR(40) NOT NULL,
    TecnicoId        INT NOT NULL CONSTRAINT FK_Tarea_Tecnico REFERENCES seg.Usuario (UsuarioId),
    Orden            INT NOT NULL,
    -- PRY-07: el cronograma usa el periodo de la tarea
    FechaInicio      DATE NOT NULL CONSTRAINT DF_Tarea_Inicio DEFAULT (CAST(SYSDATETIME() AS DATE)),
    FechaTerminacion DATE NOT NULL,
    Avance           INT NOT NULL CONSTRAINT DF_Tarea_Avance DEFAULT (0) CONSTRAINT CK_Tarea_Avance CHECK (Avance BETWEEN 0 AND 100),
    Estado           VARCHAR(20) NOT NULL CONSTRAINT CK_Tarea_Estado CHECK (Estado IN ('Pendiente','EnRevision','EnCurso','Completada')),
    CreadoPorId      INT NOT NULL CONSTRAINT FK_Tarea_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro    DATETIME2(0) NOT NULL CONSTRAINT DF_Tarea_Registro DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Tarea_Fechas CHECK (FechaTerminacion >= FechaInicio)
);
