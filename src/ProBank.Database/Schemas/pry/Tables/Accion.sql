-- PRY-09: acciones de Portafolio cuyos roles se parametrizan (Portafolio › Parametrización).
CREATE TABLE [pry].[Accion]
(
    Clave  VARCHAR(40)   NOT NULL CONSTRAINT PK_PryAccion PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Orden  INT           NOT NULL,
    Estado VARCHAR(20)   NOT NULL CONSTRAINT DF_PryAccion_Estado DEFAULT ('Activo')
);
