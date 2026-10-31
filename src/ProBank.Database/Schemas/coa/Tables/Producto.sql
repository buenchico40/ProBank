-- Productos del cliente con su saldo referencial. En esta fase no hay conexión al core: se registran manualmente.
CREATE TABLE [coa].[Producto]
(
    ProductoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Producto PRIMARY KEY,
    ClienteId        INT NOT NULL CONSTRAINT FK_Producto_Cliente REFERENCES coa.Cliente (ClienteId),
    Tipo             VARCHAR(40)   NOT NULL CONSTRAINT CK_Producto_Tipo CHECK (Tipo IN ('Cuenta de ahorros', 'Cuenta corriente', 'Depósito a plazo fijo', 'Inversión')),
    Numero           VARCHAR(30)   NOT NULL,
    SaldoReferencial DECIMAL(18,2) NOT NULL CONSTRAINT CK_Producto_Saldo CHECK (SaldoReferencial >= 0),
    CONSTRAINT UQ_Producto_Cuenta UNIQUE (ClienteId, Numero)
);
