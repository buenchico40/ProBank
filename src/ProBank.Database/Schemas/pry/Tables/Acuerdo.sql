-- PRY-08: acuerdo de la revisión semanal con responsable y fecha de cumplimiento. "Vencido" se calcula (Pendiente y fecha pasada).
CREATE TABLE [pry].[Acuerdo]
(
AcuerdoId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Acuerdo PRIMARY KEY,
    Codigo            AS ('ACU-' + RIGHT('0000' + CAST(AcuerdoId AS VARCHAR(10)), 4)) PERSISTED,
    RevisionId        INT NOT NULL CONSTRAINT FK_Acuerdo_Revision REFERENCES pry.Revision (RevisionId),
    ItemId            INT NOT NULL CONSTRAINT FK_Acuerdo_Item REFERENCES pry.Item (ItemId),
    Descripcion       NVARCHAR(500) NOT NULL,
    ResponsableId     INT NOT NULL CONSTRAINT FK_Acuerdo_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaCumplimiento DATE NOT NULL,
    Estado            VARCHAR(20) NOT NULL CONSTRAINT DF_Acuerdo_Estado DEFAULT ('Pendiente') CONSTRAINT CK_Acuerdo_Estado CHECK (Estado IN ('Pendiente','Cumplido')),
    FechaCumplido     DATE NULL
);
