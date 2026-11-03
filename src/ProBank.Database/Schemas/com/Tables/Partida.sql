-- Partida del presupuesto de un área (com.Area) en un año (PRE-2026-01).
-- COM-01: el año siguiente nace en Borrador copiado del vigente y se aprueba partida por partida (Activo); al terminar el año
-- se cierra. Ajustes, comprometido, pagado y disponible: com.vw_Partida.
CREATE TABLE [com].[Partida]
(
    PartidaId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Partida PRIMARY KEY,
    Token           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Partida_Token DEFAULT (NEWID()) CONSTRAINT UQ_Partida_Token UNIQUE,
    Codigo          VARCHAR(20)   NOT NULL CONSTRAINT UQ_Partida_Codigo UNIQUE,
    Anio            SMALLINT      NOT NULL CONSTRAINT CK_Partida_Anio CHECK (Anio BETWEEN 2000 AND 2100),
    Area            VARCHAR(30)   NOT NULL CONSTRAINT FK_Partida_Area REFERENCES com.Area (Codigo),
    Rubro           NVARCHAR(150) NOT NULL,
    Asignado        DECIMAL(18,2) NOT NULL CONSTRAINT CK_Partida_Asignado CHECK (Asignado >= 0),
    Estado          VARCHAR(20)   NOT NULL CONSTRAINT DF_Partida_Estado DEFAULT ('Borrador')
                    CONSTRAINT CK_Partida_Estado CHECK (Estado IN ('Borrador', 'Activo', 'Cerrado')),
    CreadoPorId     INT           NOT NULL CONSTRAINT FK_Partida_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Partida_Registro DEFAULT (SYSDATETIME()),
    AprobadoPorId   INT           NULL CONSTRAINT FK_Partida_Aprobador REFERENCES seg.Usuario (UsuarioId),
    FechaAprobacion DATETIME2(0)  NULL,
    CONSTRAINT UQ_Partida_Rubro UNIQUE (Anio, Area, Rubro)
);
