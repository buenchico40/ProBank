-- Áreas que llevan presupuesto (TI, Marketing, Operaciones…). Se administran en Presupuesto › Áreas del presupuesto.
-- El código (TI) no cambia porque lo usan las partidas; un área inactiva conserva sus partidas pero no recibe nuevas ni prepara borrador.
CREATE TABLE [com].[Area]
(
    AreaId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Area PRIMARY KEY,
    Codigo          VARCHAR(30)   NOT NULL CONSTRAINT UQ_Area_Codigo UNIQUE
                    CONSTRAINT CK_Area_Codigo CHECK (Codigo NOT LIKE '%[^A-Z0-9]%' AND LEN(Codigo) BETWEEN 2 AND 30),
    Nombre          NVARCHAR(100) NOT NULL CONSTRAINT UQ_Area_Nombre UNIQUE,
    Activa          BIT           NOT NULL CONSTRAINT DF_Area_Activa DEFAULT (1),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Area_Registro DEFAULT (SYSDATETIME())
);
