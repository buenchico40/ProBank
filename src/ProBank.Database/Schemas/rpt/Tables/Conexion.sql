CREATE TABLE [rpt].[Conexion]
(
ConexionId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Conexion PRIMARY KEY,
    Codigo         VARCHAR(20) NOT NULL CONSTRAINT UQ_Conexion UNIQUE,
    Nombre         NVARCHAR(150) NOT NULL,
    Servidor       VARCHAR(200) NOT NULL,
    BaseDatos      VARCHAR(128) NOT NULL,
    Autenticacion  VARCHAR(40) NOT NULL,
    -- SQL-02: solo el NOMBRE del secreto en Key Vault; la cadena nunca se guarda aquí
    SecretoKeyVault VARCHAR(127) NOT NULL,
    Estado         VARCHAR(20) NOT NULL CONSTRAINT DF_Conexion_Estado DEFAULT ('Activo')
);
