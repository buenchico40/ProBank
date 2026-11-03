-- MKT-01: imagen promocional del carrusel del Panel o de la pantalla de inicio de sesión, con vigencia, audiencia y orden.
-- La imagen (JPG o PNG) vive en Blob Storage, contenedor marketing, en ImagenRuta. Estado guardado: Borrador, Publicado o Retirado;
-- lo publicado se muestra como Programado, Publicado o Vencido según la vigencia (mkt.fn_EstadoVigente).
CREATE TABLE [mkt].[Promocion]
(
    PromocionId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promocion PRIMARY KEY,
    Token            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Promocion_Token DEFAULT (NEWID()) CONSTRAINT UQ_Promocion_Token UNIQUE,
    Codigo           AS ('PRM-' + RIGHT('000' + CAST(PromocionId AS VARCHAR(10)), 3)) PERSISTED,
    Titulo           NVARCHAR(200) NOT NULL,
    Ubicacion        NVARCHAR(20)  NOT NULL CONSTRAINT CK_Promocion_Ubicacion CHECK (Ubicacion IN (N'Panel principal', N'Inicio de sesión')),
    ImagenRuta       VARCHAR(300)  NULL,
    ImagenTipo       VARCHAR(30)   NULL,
    TextoAlternativo NVARCHAR(250) NULL,
    Enlace           NVARCHAR(400) NULL,
    Desde            DATE          NULL,
    Hasta            DATE          NULL,
    Audiencia        NVARCHAR(60)  NOT NULL CONSTRAINT DF_Promocion_Audiencia DEFAULT (N'Todos'),
    Orden            INT           NOT NULL CONSTRAINT CK_Promocion_Orden CHECK (Orden >= 1),
    Estado           VARCHAR(20)   NOT NULL CONSTRAINT DF_Promocion_Estado DEFAULT ('Borrador')
                     CONSTRAINT CK_Promocion_Estado CHECK (Estado IN ('Borrador', 'Publicado', 'Retirado')),
    CreadoPorId      INT           NOT NULL CONSTRAINT FK_Promocion_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro    DATETIME2(0)  NOT NULL CONSTRAINT DF_Promocion_Registro DEFAULT (SYSDATETIME()),
    PublicadoPorId   INT           NULL CONSTRAINT FK_Promocion_Publicador REFERENCES seg.Usuario (UsuarioId),
    FechaPublicacion DATETIME2(0)  NULL,
    CONSTRAINT CK_Promocion_Vigencia CHECK (Hasta IS NULL OR Desde IS NULL OR Hasta >= Desde)
);
