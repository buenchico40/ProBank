-- SEG-03: opciones del menú (las mismas del prototipo). Clave estable que usan la API, la Web y los SP.
CREATE TABLE [seg].[Opcion]
(
    Opcion  VARCHAR(60)   NOT NULL CONSTRAINT PK_Opcion PRIMARY KEY,
    Modulo  NVARCHAR(60)  NOT NULL,
    Nombre  NVARCHAR(100) NOT NULL,
    Orden   INT           NOT NULL
);
