-- CFG-01: parámetros globales de la solución por categoría (correo, consultas dinámicas…). La definición (nombre, tipo, rango,
-- opciones) viene del despliegue (Scripts/Seed.PostDeployment.sql); el valor lo administra Configuración › Parámetros generales.
-- CFG-02: un parámetro de tipo Secreto guarda solo el NOMBRE de un secreto de Key Vault, con el prefijo exigido (smtp-…).
CREATE TABLE [cfg].[Parametro]
(
    Clave             VARCHAR(100)   NOT NULL CONSTRAINT PK_CfgParametro PRIMARY KEY,
    Categoria         NVARCHAR(60)   NOT NULL,
    Nombre            NVARCHAR(150)  NOT NULL,
    Descripcion       NVARCHAR(500)  NULL,
    Tipo              VARCHAR(10)    NOT NULL CONSTRAINT CK_CfgParametro_Tipo CHECK (Tipo IN ('Texto', 'Numero', 'Lista', 'Correo', 'Secreto')),
    Opciones          NVARCHAR(500)  NULL,     -- lista: separadas por coma
    Minimo            INT            NULL,     -- número
    Maximo            INT            NULL,
    Prefijo           VARCHAR(20)    NULL,     -- secreto: prefijo obligatorio del nombre
    Valor             NVARCHAR(1000) NULL,
    Orden             INT            NOT NULL,
    ModificadoPorId   INT            NULL CONSTRAINT FK_CfgParametro_Usuario REFERENCES seg.Usuario (UsuarioId),
    FechaModificacion DATETIME2(0)   NULL
);
