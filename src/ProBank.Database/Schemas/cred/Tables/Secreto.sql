CREATE TABLE [cred].[Secreto]
(
SecretoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Secreto PRIMARY KEY,
    GrupoId   INT NOT NULL CONSTRAINT FK_Secreto_Grupo REFERENCES cred.GrupoAcceso (GrupoId),
    Nombre    NVARCHAR(150) NOT NULL,
    -- CRE-03: cifrado con Always Encrypted (la CEK se crea al desplegar; ver docs/runbooks/rotacion-llaves.md)
    Valor     NVARCHAR(4000) NOT NULL,
    Vence     DATE NULL,
    DiasAviso INT NOT NULL CONSTRAINT DF_Secreto_Aviso DEFAULT (30)
);
