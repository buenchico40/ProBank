-- Entidades que emiten coactivas. El RUC identifica a la entidad en la carga masiva (COA-04).
CREATE TABLE [coa].[Entidad]
(
    EntidadId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Entidad PRIMARY KEY,
    Ruc            VARCHAR(13)   NOT NULL CONSTRAINT UQ_Entidad_Ruc UNIQUE,
    Nombre         NVARCHAR(150) NOT NULL,
    -- Cuenta a la que se transfieren los fondos (se propone al registrar una transferencia)
    CuentaDestino  NVARCHAR(150) NULL,
    Estado         VARCHAR(20)   NOT NULL CONSTRAINT DF_Entidad_Estado DEFAULT ('Activo')
);
