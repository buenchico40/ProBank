CREATE TABLE [rpt].[ConsultaAprobador]
(
ConsultaId INT NOT NULL CONSTRAINT FK_ConsultaAprobador_Consulta REFERENCES rpt.Consulta (ConsultaId),
    UsuarioId INT NOT NULL CONSTRAINT FK_CA_Usuario REFERENCES seg.Usuario (UsuarioId),
    CONSTRAINT PK_ConsultaAprobador PRIMARY KEY (ConsultaId, UsuarioId)
);
