-- CRE-01: renovaciones registradas de un certificado (vencimiento y huella anteriores y nuevos).
CREATE TABLE [cred].[CertificadoRenovacion]
(
    RenovacionId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CertificadoRenovacion PRIMARY KEY,
    CertificadoId  INT          NOT NULL CONSTRAINT FK_CertRenovacion_Certificado REFERENCES cred.Certificado (CertificadoId),
    Fecha          DATETIME2(0) NOT NULL CONSTRAINT DF_CertRenovacion_Fecha DEFAULT (SYSDATETIME()),
    VenceAnterior  DATE         NOT NULL,
    VenceNuevo     DATE         NOT NULL,
    HuellaAnterior VARCHAR(64)  NULL,
    HuellaNueva    VARCHAR(64)  NOT NULL,
    UsuarioId      INT          NOT NULL CONSTRAINT FK_CertRenovacion_Usuario REFERENCES seg.Usuario (UsuarioId)
);
