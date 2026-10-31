CREATE TABLE [pry].[TareaAprobador]
(
TareaId   INT NOT NULL CONSTRAINT FK_TareaAprobador_Tarea REFERENCES pry.Tarea (TareaId),
    UsuarioId INT NOT NULL CONSTRAINT FK_TareaAprobador_Usuario REFERENCES seg.Usuario (UsuarioId),
    CONSTRAINT PK_TareaAprobador PRIMARY KEY (TareaId, UsuarioId)
);
