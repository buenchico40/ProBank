CREATE TABLE [ver].[Dependencia]
(
ComponenteId  INT NOT NULL CONSTRAINT FK_Dep_Componente REFERENCES ver.Componente (ComponenteId),
    DependeDeId   INT NOT NULL CONSTRAINT FK_Dep_DependeDe REFERENCES ver.Componente (ComponenteId),
    VersionMinima VARCHAR(20) NOT NULL,
    CONSTRAINT PK_Dependencia PRIMARY KEY (ComponenteId, DependeDeId)
);
