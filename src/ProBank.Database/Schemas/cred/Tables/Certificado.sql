-- CRE-01: certificado digital. ProBank solo registra su vigencia: el certificado y su clave privada quedan en el almacén indicado.
CREATE TABLE [cred].[Certificado]
(
    CertificadoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Certificado PRIMARY KEY,
    Token               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Certificado_Token DEFAULT (NEWID()) CONSTRAINT UQ_Certificado_Token UNIQUE,
    Codigo              VARCHAR(10)   NOT NULL CONSTRAINT UQ_Certificado_Codigo UNIQUE,
    NombreComun         NVARCHAR(200) NOT NULL,
    Tipo                NVARCHAR(30)  NOT NULL CONSTRAINT CK_Certificado_Tipo CHECK (Tipo IN (N'SSL/TLS', N'Firma de código', N'Cliente', N'Firma SAML')),
    Emisor              NVARCHAR(150) NOT NULL,
    Vence               DATE          NOT NULL,
    Almacen             NVARCHAR(150) NOT NULL,
    Responsable         NVARCHAR(100) NOT NULL,
    DiasAviso           INT           NOT NULL CONSTRAINT CK_Certificado_Aviso CHECK (DiasAviso BETWEEN 1 AND 365),
    Huella              VARCHAR(64)   NULL,
    NombresAlternativos NVARCHAR(500) NULL,
    UsadoPor            NVARCHAR(200) NULL,
    Activo              BIT           NOT NULL CONSTRAINT DF_Certificado_Activo DEFAULT (1),
    FechaRegistro       DATETIME2(0)  NOT NULL CONSTRAINT DF_Certificado_Registro DEFAULT (SYSDATETIME())
);
