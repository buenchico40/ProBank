-- CRE-01 y CRE-03: versiones de la contraseña de una cuenta técnica o del valor de un secreto. El valor va cifrado con Always
-- Encrypted (RANDOMIZED; la CMK está en Key Vault): la base y el DBA solo ven bytes cifrados; lo descifra la API, que tiene la llave.
-- Solo la versión vigente conserva el valor; las anteriores quedan como historial (fecha, quién y nota) sin valor.
CREATE TABLE [cred].[CredencialVersion]
(
    VersionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CredencialVersion PRIMARY KEY,
    CuentaId  INT NULL CONSTRAINT FK_CredVersion_Cuenta REFERENCES cred.CuentaTecnica (CuentaId),
    SecretoId INT NULL CONSTRAINT FK_CredVersion_Secreto REFERENCES cred.Secreto (SecretoId),
    Version   INT NOT NULL,
    Valor     NVARCHAR(4000) COLLATE Latin1_General_BIN2
              ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = [CEK_ProBank], ENCRYPTION_TYPE = RANDOMIZED, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256') NULL,
    Fecha     DATETIME2(0)  NOT NULL CONSTRAINT DF_CredVersion_Fecha DEFAULT (SYSDATETIME()),
    Vence     DATE          NULL,
    UsuarioId INT           NOT NULL CONSTRAINT FK_CredVersion_Usuario REFERENCES seg.Usuario (UsuarioId),
    Nota      NVARCHAR(200) NULL,
    CONSTRAINT CK_CredVersion_Duenio CHECK ((CuentaId IS NULL AND SecretoId IS NOT NULL) OR (CuentaId IS NOT NULL AND SecretoId IS NULL))
);
GO
CREATE UNIQUE INDEX UX_CredVersion_Cuenta ON cred.CredencialVersion (CuentaId, Version) WHERE CuentaId IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_CredVersion_Secreto ON cred.CredencialVersion (SecretoId, Version) WHERE SecretoId IS NOT NULL;
