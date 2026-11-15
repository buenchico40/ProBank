-- DEC-07: firmas de versiones anteriores de una decisión. Cuando un aprobador la modifica, las firmas de la versión vigente pasan
-- aquí (con su sello, que sigue verificable contra la huella de su versión) y todos vuelven a firmar la versión nueva.
CREATE TABLE [dec].[FirmaAnterior]
(
    DecisionId INT           NOT NULL,
    Version    INT           NOT NULL,
    UsuarioId  INT           NOT NULL CONSTRAINT FK_FirmaAnterior_Usuario REFERENCES seg.Usuario (UsuarioId),
    Resultado  NVARCHAR(10)  NOT NULL,
    Comentario NVARCHAR(500) NULL,
    Fecha      DATETIME2(0)  NOT NULL,
    Ip         VARCHAR(45)   NULL,
    Sello      VARBINARY(32) NOT NULL,
    CONSTRAINT PK_FirmaAnterior PRIMARY KEY (DecisionId, Version, UsuarioId),
    CONSTRAINT FK_FirmaAnterior_Version FOREIGN KEY (DecisionId, Version) REFERENCES dec.DecisionVersion (DecisionId, Version)
);
