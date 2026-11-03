-- Servicio que se renueva (REN-0001). COM-02: presupuestado (con partida) o no. COM-03: se avisa DiasAviso días antes de Vence.
-- Estado guardado: Activo, Requerimiento enviado o Retirado; se muestra Vigente, Por vencer o Vencido según la fecha (com.usp_Renovacion_Listar).
CREATE TABLE [com].[Renovacion]
(
    RenovacionId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Renovacion PRIMARY KEY,
    Token         UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Renovacion_Token DEFAULT (NEWID()) CONSTRAINT UQ_Renovacion_Token UNIQUE,
    Codigo        AS ('REN-' + RIGHT('0000' + CAST(RenovacionId AS VARCHAR(10)), 4)) PERSISTED,
    Servicio      NVARCHAR(150) NOT NULL,
    Proveedor     NVARCHAR(150) NOT NULL,
    Vence         DATE          NOT NULL,
    MontoEstimado DECIMAL(18,2) NOT NULL CONSTRAINT CK_Renovacion_Monto CHECK (MontoEstimado >= 0),
    Presupuestado BIT           NOT NULL,
    PartidaId     INT NULL CONSTRAINT FK_Renovacion_Partida REFERENCES com.Partida (PartidaId),
    DiasAviso     INT           NOT NULL CONSTRAINT CK_Renovacion_Aviso CHECK (DiasAviso BETWEEN 1 AND 365),
    Estado        VARCHAR(30)   NOT NULL CONSTRAINT DF_Renovacion_Estado DEFAULT ('Activo')
                  CONSTRAINT CK_Renovacion_Estado CHECK (Estado IN ('Activo', 'Requerimiento enviado', 'Retirado')),
    CreadoPorId   INT           NOT NULL CONSTRAINT FK_Renovacion_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_Renovacion_Registro DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Renovacion_Partida CHECK ((Presupuestado = 1 AND PartidaId IS NOT NULL) OR (Presupuestado = 0 AND PartidaId IS NULL))
);
