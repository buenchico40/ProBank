CREATE TABLE [rpt].[ProgramacionDestinatario]
(
ProgramacionId INT NOT NULL CONSTRAINT FK_PD_Programacion REFERENCES rpt.Programacion (ProgramacionId),
    EntraObjectId  UNIQUEIDENTIFIER NOT NULL,
    Correo         VARCHAR(256) NOT NULL,
    CONSTRAINT PK_ProgramacionDestinatario PRIMARY KEY (ProgramacionId, EntraObjectId)
);
