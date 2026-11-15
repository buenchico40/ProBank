-- DEC-02: tipo de decisión (compra, contratación, excepción de política…) con su flujo de aprobación: en secuencia (por orden) o
-- todos a la vez. Los aprobadores van en dec.TipoAprobador.
CREATE TABLE [dec].[TipoDecision]
(
    TipoId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TipoDecision PRIMARY KEY,
    Codigo        VARCHAR(10)   NOT NULL CONSTRAINT UQ_TipoDecision_Codigo UNIQUE,
    Nombre        NVARCHAR(100) NOT NULL CONSTRAINT UQ_TipoDecision UNIQUE,
    Descripcion   NVARCHAR(300) NULL,
    Secuencial    BIT           NOT NULL CONSTRAINT DF_TipoDecision_Secuencial DEFAULT (0),
    Activo        BIT           NOT NULL CONSTRAINT DF_TipoDecision_Activo DEFAULT (1),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_TipoDecision_Registro DEFAULT (SYSDATETIME())
);
