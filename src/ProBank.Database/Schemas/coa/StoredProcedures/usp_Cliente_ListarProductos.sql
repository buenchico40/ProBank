-- Productos de un cliente con lo bloqueado por todas sus coactivas y lo disponible.
CREATE PROCEDURE coa.usp_Cliente_ListarProductos
    @Identificacion VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Tipo, p.Numero, p.SaldoReferencial,
           ISNULL((SELECT SUM(b.Vigente) FROM coa.vw_Bloqueo b WHERE b.ProductoId = p.ProductoId), 0) AS Bloqueado
    FROM coa.Producto p JOIN coa.Cliente cl ON cl.ClienteId = p.ClienteId
    WHERE cl.Identificacion = @Identificacion
    ORDER BY p.Tipo, p.Numero;
END;
