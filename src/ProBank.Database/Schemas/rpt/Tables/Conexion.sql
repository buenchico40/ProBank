-- SQL-02: bases de datos donde se ejecutan las consultas (CNX-001). Solo se guarda el NOMBRE del secreto de Key Vault que
-- contiene la cadena de conexión: la API la lee al ejecutar y nunca viaja al navegador ni queda en esta tabla.
CREATE TABLE [rpt].[Conexion]
(
    ConexionId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Conexion PRIMARY KEY,
    Codigo          VARCHAR(20)   NOT NULL CONSTRAINT UQ_Conexion UNIQUE,
    Nombre          NVARCHAR(150) NOT NULL CONSTRAINT UQ_Conexion_Nombre UNIQUE,
    Servidor        VARCHAR(200)  NOT NULL,
    BaseDatos       VARCHAR(128)  NOT NULL,
    Autenticacion   VARCHAR(40)   NOT NULL CONSTRAINT CK_Conexion_Autenticacion
                    CHECK (Autenticacion IN ('Usuario SQL de solo lectura', 'Identidad administrada (Entra ID)')),
    SecretoKeyVault VARCHAR(127)  NOT NULL CONSTRAINT CK_Conexion_Secreto CHECK (SecretoKeyVault LIKE 'cnx-%' AND SecretoKeyVault NOT LIKE '%[^A-Za-z0-9-]%'),
    Estado          VARCHAR(20)   NOT NULL CONSTRAINT DF_Conexion_Estado DEFAULT ('Activo')
                    CONSTRAINT CK_Conexion_Estado CHECK (Estado IN ('Activo', 'Inactivo')),
    FechaRegistro   DATETIME2(0)  NOT NULL CONSTRAINT DF_Conexion_Registro DEFAULT (SYSDATETIME())
);
