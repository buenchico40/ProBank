CREATE TABLE [ver].[VersionItem]
(
VersionId INT NOT NULL CONSTRAINT FK_VersionItem_Version REFERENCES ver.Version (VersionId),
    ItemId    INT NOT NULL CONSTRAINT FK_VersionItem_Item REFERENCES pry.Item (ItemId),
    CONSTRAINT PK_VersionItem PRIMARY KEY (VersionId, ItemId)
);
