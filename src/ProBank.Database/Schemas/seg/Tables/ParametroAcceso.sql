-- Parámetros de acceso (Seguridad › Parámetros de acceso). Valores numéricos guardados como texto.
CREATE TABLE [seg].[ParametroAcceso]
(
    Clave       VARCHAR(60)   NOT NULL CONSTRAINT PK_ParametroAcceso PRIMARY KEY,
    Valor       NVARCHAR(200) NOT NULL,
    Nombre      NVARCHAR(150) NOT NULL,
    Minimo      INT           NOT NULL,
    Maximo      INT           NOT NULL,
    Orden       INT           NOT NULL
);
