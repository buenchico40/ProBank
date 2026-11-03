-- Documentos de compras en Blob Storage (contenedor adjuntos): el contrato de una solicitud y las facturas digitales (PDF/XML)
-- y el documento de distribución de un pago.
CREATE TABLE [com].[Documento]
(
    DocumentoId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ComDocumento PRIMARY KEY,
    Token         UNIQUEIDENTIFIER NOT NULL CONSTRAINT UQ_ComDocumento_Token UNIQUE,
    SolicitudId   INT NULL CONSTRAINT FK_ComDocumento_Solicitud REFERENCES com.Solicitud (SolicitudId),
    PagoId        INT NULL CONSTRAINT FK_ComDocumento_Pago REFERENCES com.Pago (PagoId),
    Tipo          NVARCHAR(20)  NOT NULL CONSTRAINT CK_ComDocumento_Tipo CHECK (Tipo IN (N'Contrato', N'Factura', N'Distribución')),
    Nombre        NVARCHAR(200) NOT NULL,
    TipoContenido VARCHAR(100)  NOT NULL,
    Tamano        BIGINT        NOT NULL CONSTRAINT CK_ComDocumento_Tamano CHECK (Tamano > 0),
    Ruta          VARCHAR(300)  NOT NULL,
    SubidoPorId   INT           NOT NULL CONSTRAINT FK_ComDocumento_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_ComDocumento_Registro DEFAULT (SYSDATETIME()),
    -- El contrato es de la solicitud; facturas y distribución, del pago
    CONSTRAINT CK_ComDocumento_Dueno CHECK ((Tipo = N'Contrato' AND SolicitudId IS NOT NULL AND PagoId IS NULL)
                                         OR (Tipo <> N'Contrato' AND PagoId IS NOT NULL AND SolicitudId IS NULL))
);
