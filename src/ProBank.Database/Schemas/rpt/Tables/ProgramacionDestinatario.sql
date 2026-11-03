-- SQL-08: destinatarios del envío programado (correo; EntraObjectId cuando es un usuario o grupo de Entra ID).
CREATE TABLE [rpt].[ProgramacionDestinatario]
(
    ProgramacionId INT NOT NULL CONSTRAINT FK_PD_Programacion REFERENCES rpt.Programacion (ProgramacionId),
    Correo         VARCHAR(256) NOT NULL,
    EntraObjectId  UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_ProgramacionDestinatario PRIMARY KEY (ProgramacionId, Correo)
);
