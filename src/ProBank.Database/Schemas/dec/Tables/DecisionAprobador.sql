-- DEC-03: aprobadores de una decisión (propuestos por su tipo y los que agrega quien la registró) y su firma: resultado,
-- comentario, fecha, IP y sello SHA-256 (dec.fn_Sello) que une la huella del contenido con quién firmó, qué decidió y cuándo.
-- Una vez que alguno firmó, los aprobadores ya no se quitan: solo se agregan.
CREATE TABLE [dec].[DecisionAprobador]
(
    DecisionId    INT NOT NULL CONSTRAINT FK_DecAprobador_Decision REFERENCES dec.Decision (DecisionId),
    UsuarioId     INT NOT NULL CONSTRAINT FK_DecAprobador_Usuario REFERENCES seg.Usuario (UsuarioId),
    Orden         INT NOT NULL CONSTRAINT CK_DecAprobador_Orden CHECK (Orden BETWEEN 1 AND 99),
    Origen        NVARCHAR(10) NOT NULL CONSTRAINT DF_DecAprobador_Origen DEFAULT (N'Tipo') CONSTRAINT CK_DecAprobador_Origen CHECK (Origen IN (N'Tipo', N'Agregado')),
    AgregadoPorId INT NOT NULL CONSTRAINT FK_DecAprobador_AgregadoPor REFERENCES seg.Usuario (UsuarioId),
    FechaAgregado DATETIME2(0) NOT NULL CONSTRAINT DF_DecAprobador_Agregado DEFAULT (SYSDATETIME()),
    Resultado     NVARCHAR(10)  NOT NULL CONSTRAINT DF_DecAprobador_Resultado DEFAULT (N'Pendiente')
                  CONSTRAINT CK_DecAprobador_Resultado CHECK (Resultado IN (N'Pendiente', N'Aprobada', N'Rechazada')),
    Comentario    NVARCHAR(500) NULL,
    Fecha         DATETIME2(0)  NULL,
    Ip            VARCHAR(45)   NULL,
    Sello         VARBINARY(32) NULL,
    CONSTRAINT PK_DecisionAprobador PRIMARY KEY (DecisionId, UsuarioId),
    CONSTRAINT CK_DecAprobador_Firma CHECK (Resultado = N'Pendiente' OR (Fecha IS NOT NULL AND Sello IS NOT NULL))
);
