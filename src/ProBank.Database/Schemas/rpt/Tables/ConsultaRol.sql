CREATE TABLE [rpt].[ConsultaRol]
(
ConsultaId INT NOT NULL CONSTRAINT FK_ConsultaRol_Consulta REFERENCES rpt.Consulta (ConsultaId),
    RolId INT NOT NULL CONSTRAINT FK_CR_Rol REFERENCES seg.Rol (RolId),
    CONSTRAINT PK_ConsultaRol PRIMARY KEY (ConsultaId, RolId)
);
