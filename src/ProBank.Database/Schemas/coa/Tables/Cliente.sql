-- Cliente o empresa contra quien se emite la coactiva. La identificación se guarda como texto (conserva ceros a la izquierda).
CREATE TABLE [coa].[Cliente]
(
    ClienteId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cliente PRIMARY KEY,
    TipoIdentificacion VARCHAR(10)   NOT NULL CONSTRAINT CK_Cliente_TipoId CHECK (TipoIdentificacion IN ('Cédula', 'RUC', 'Pasaporte')),
    Identificacion     VARCHAR(20)   NOT NULL CONSTRAINT UQ_Cliente_Identificacion UNIQUE,
    Nombre             NVARCHAR(150) NOT NULL
);
