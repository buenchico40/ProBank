CREATE TABLE [pry].[Aprobacion]
(
AprobacionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Aprobacion PRIMARY KEY,
    TareaId      INT NOT NULL CONSTRAINT FK_Aprobacion_Tarea REFERENCES pry.Tarea (TareaId),
    AprobadorId  INT NOT NULL CONSTRAINT FK_Aprobacion_Usuario REFERENCES seg.Usuario (UsuarioId),
    Ronda        INT NOT NULL CONSTRAINT DF_Aprobacion_Ronda DEFAULT (1),
    Decision     VARCHAR(10) NOT NULL CONSTRAINT CK_Aprobacion_Decision CHECK (Decision IN ('Aprobada','Rechazada')),
    Comentario   NVARCHAR(1000) NULL,
    Fecha        DATETIME2(0) NOT NULL CONSTRAINT DF_Aprobacion_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Aprobacion_Comentario CHECK (Decision = 'Aprobada' OR LEN(Comentario) > 0)
);
