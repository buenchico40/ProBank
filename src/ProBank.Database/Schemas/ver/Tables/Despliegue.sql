CREATE TABLE [ver].[Despliegue]
(
DespliegueId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Despliegue PRIMARY KEY,
    VersionId       INT NOT NULL CONSTRAINT FK_Despliegue_Version REFERENCES ver.Version (VersionId),
    Ambiente        VARCHAR(15) NOT NULL CONSTRAINT CK_Despliegue_Amb CHECK (Ambiente IN ('Desarrollo','Pruebas','Produccion')),
    Fecha           DATETIME2(0) NOT NULL,
    ResponsableId   INT NOT NULL,
    Resultado       VARCHAR(20) NOT NULL,
    MotivoReversion NVARCHAR(500) NULL,
    CONSTRAINT CK_Despliegue_Reversion CHECK (Resultado <> 'Revertido' OR LEN(MotivoReversion) > 0)
);
