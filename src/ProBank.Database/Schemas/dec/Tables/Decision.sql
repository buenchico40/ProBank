-- DEC-01: decisión del banco (el alcance, montos incluidos, va en la descripción; los respaldos, en dec.DecisionAdjunto).
-- Borrador → En aprobación (se fija la huella SHA-256 de la información registrada) → Aprobada, Rechazada o Anulada.
-- Enviada, su contenido solo cambia si un aprobador lo modifica: nace una versión nueva con otra huella y todos vuelven a firmar
-- (dec.DecisionVersion guarda cada versión y dec.FirmaAnterior las firmas de las versiones anteriores).
CREATE TABLE [dec].[Decision]
(
    DecisionId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Decision PRIMARY KEY,
    Token            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Decision_Token DEFAULT (NEWID()) CONSTRAINT UQ_Decision_Token UNIQUE,
    Codigo           VARCHAR(12)   NOT NULL CONSTRAINT UQ_Decision_Codigo UNIQUE,
    TipoId           INT           NOT NULL CONSTRAINT FK_Decision_Tipo REFERENCES dec.TipoDecision (TipoId),
    Titulo           NVARCHAR(200) NOT NULL,
    Descripcion      NVARCHAR(MAX) NOT NULL,
    Area             NVARCHAR(100) NOT NULL,
    Fecha            DATE          NOT NULL,
    Estado           NVARCHAR(20)  NOT NULL CONSTRAINT DF_Decision_Estado DEFAULT (N'Borrador')
                     CONSTRAINT CK_Decision_Estado CHECK (Estado IN (N'Borrador', N'En aprobación', N'Aprobada', N'Rechazada', N'Anulada')),
    Secuencial       BIT           NOT NULL CONSTRAINT DF_Decision_Secuencial DEFAULT (0),
    Version          INT           NOT NULL CONSTRAINT DF_Decision_Version DEFAULT (1),
    Huella           VARBINARY(32) NULL,
    CreadoPorId      INT           NOT NULL CONSTRAINT FK_Decision_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro    DATETIME2(0)  NOT NULL CONSTRAINT DF_Decision_Registro DEFAULT (SYSDATETIME()),
    FechaEnvio       DATETIME2(0)  NULL,
    FechaCierre      DATETIME2(0)  NULL,
    MotivoAnulacion  NVARCHAR(500) NULL
);
