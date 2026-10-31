CREATE TABLE [pry].[ItemResponsable]
(
ItemId    INT NOT NULL CONSTRAINT FK_ItemResp_Item REFERENCES pry.Item (ItemId),
    UsuarioId INT NOT NULL CONSTRAINT FK_ItemResp_Usuario REFERENCES seg.Usuario (UsuarioId),
    CONSTRAINT PK_ItemResponsable PRIMARY KEY (ItemId, UsuarioId)
);
