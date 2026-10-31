-- PRY-08: revisión semanal de un ítem. Los temas tratados quedan en la bitácora; los acuerdos cuelgan de la revisión.
CREATE TABLE [pry].[Revision]
(
    RevisionId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Revision PRIMARY KEY,
    ItemId        INT NOT NULL CONSTRAINT FK_Revision_Item REFERENCES pry.Item (ItemId),
    Fecha         DATE NOT NULL,
    Temas         NVARCHAR(2000) NULL,
    UsuarioId     INT NOT NULL CONSTRAINT FK_Revision_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Revision_Registro DEFAULT (SYSDATETIME())
);
