CREATE TABLE [rpt].[ConsultaConexion]
(
ConsultaId INT NOT NULL CONSTRAINT FK_ConsultaConexion_Consulta REFERENCES rpt.Consulta (ConsultaId),
    ConexionId INT NOT NULL CONSTRAINT FK_CC_Conexion REFERENCES rpt.Conexion (ConexionId),
    CONSTRAINT PK_ConsultaConexion PRIMARY KEY (ConsultaId, ConexionId)
);
