-- Proyecto, mejora o bug. El código sale del tipo y del número (PRY-001, MEJ-002, BUG-003).
CREATE TABLE [pry].[Item]
(
ItemId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Item PRIMARY KEY,
    Token    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Item_Token DEFAULT (NEWID()) CONSTRAINT UQ_Item_Token UNIQUE,
    Tipo     VARCHAR(10) NOT NULL CONSTRAINT CK_Item_Tipo CHECK (Tipo IN ('Proyecto','Mejora','Bug')),
    Codigo   AS (CASE Tipo WHEN 'Proyecto' THEN 'PRY' WHEN 'Mejora' THEN 'MEJ' ELSE 'BUG' END + '-' + RIGHT('000' + CAST(ItemId AS VARCHAR(10)), 3)) PERSISTED,
    Titulo   NVARCHAR(200) NOT NULL,
    DescripcionHtml NVARCHAR(MAX) NULL,
    Estado   VARCHAR(20) NOT NULL CONSTRAINT DF_Item_Estado DEFAULT ('Borrador')
             CONSTRAINT CK_Item_Estado CHECK (Estado IN ('Borrador','Pendiente','En curso','En revisión','Completado','Anulado')),
    CreadoPorId   INT NULL CONSTRAINT FK_Item_Creador REFERENCES seg.Usuario (UsuarioId),
    FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Item_Registro DEFAULT (SYSDATETIME())
);
