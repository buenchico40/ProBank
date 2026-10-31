CREATE TABLE [ver].[VersionAprobador]
(
VersionId INT NOT NULL CONSTRAINT FK_VA_Version REFERENCES ver.Version (VersionId),
    UsuarioId INT NOT NULL,
    Decision  VARCHAR(10) NULL,
    Comentario NVARCHAR(1000) NULL,
    CONSTRAINT PK_VersionAprobador PRIMARY KEY (VersionId, UsuarioId)
);
