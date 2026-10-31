CREATE TABLE [mkt].[Promocion]
(
PromocionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promocion PRIMARY KEY,
    Titulo      NVARCHAR(200) NOT NULL,
    BlobUri     NVARCHAR(400) NOT NULL,
    Desde       DATE NOT NULL,
    Hasta       DATE NULL,
    Audiencia   VARCHAR(60) NOT NULL,
    Orden       INT NOT NULL,
    Estado      VARCHAR(20) NOT NULL
);
