-- Historial de la tarea (creación, edición, envío, aprobaciones, rechazos, avance y cierre). Entra en los informes de seguimiento.
CREATE TABLE [pry].[TareaEvento]
(
    EventoId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TareaEvento PRIMARY KEY,
    TareaId   INT NOT NULL CONSTRAINT FK_TareaEvento_Tarea REFERENCES pry.Tarea (TareaId),
    Fecha     DATETIME2(0) NOT NULL CONSTRAINT DF_TareaEvento_Fecha DEFAULT (SYSDATETIME()),
    UsuarioId INT NOT NULL CONSTRAINT FK_TareaEvento_Usuario REFERENCES seg.Usuario (UsuarioId),
    Evento    NVARCHAR(200) NOT NULL,
    Detalle   NVARCHAR(1000) NULL
);
GO
CREATE NONCLUSTERED INDEX IX_TareaEvento_Tarea ON pry.TareaEvento (TareaId, Fecha);
