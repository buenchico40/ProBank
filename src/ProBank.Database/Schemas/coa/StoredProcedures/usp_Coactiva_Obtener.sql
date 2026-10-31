-- Detalle de la coactiva: datos, productos del cliente (con lo bloqueado por esta y por otras coactivas),
-- sus bloqueos, sus movimientos y las órdenes de los oficios que la refieren.
CREATE PROCEDURE coa.usp_Coactiva_Obtener
    @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CoactivaId INT, @ClienteId INT;
    SELECT @CoactivaId = CoactivaId, @ClienteId = ClienteId FROM coa.Coactiva WHERE Token = @Token;

    SELECT c.Token, c.NumeroProceso, f.Numero AS Oficio, f.Token AS OficioToken, e.EntidadId, e.Nombre AS Entidad, e.CuentaDestino,
           cl.TipoIdentificacion, cl.Identificacion, cl.Nombre AS Cliente, c.Monto, c.Bloqueado, c.Levantado, c.Transferido,
           c.Cobertura, c.Estado, c.ProductosPermitidos, c.Observacion
    FROM coa.vw_Coactiva c
    JOIN coa.Oficio f ON f.OficioId = c.OficioId
    JOIN coa.Entidad e ON e.EntidadId = c.EntidadId
    JOIN coa.Cliente cl ON cl.ClienteId = c.ClienteId
    WHERE c.CoactivaId = @CoactivaId;

    SELECT p.ProductoId, p.Tipo, p.Numero, p.SaldoReferencial,
           ISNULL((SELECT SUM(b.Vigente) FROM coa.vw_Bloqueo b WHERE b.ProductoId = p.ProductoId), 0) AS BloqueadoTotal,
           ISNULL((SELECT SUM(b.Vigente) FROM coa.vw_Bloqueo b WHERE b.ProductoId = p.ProductoId AND b.CoactivaId = @CoactivaId), 0) AS BloqueadoEsta,
           (SELECT STRING_AGG(b.Estado, ',') FROM coa.vw_Bloqueo b WHERE b.ProductoId = p.ProductoId AND b.CoactivaId = @CoactivaId) AS Estados
    FROM coa.Producto p WHERE p.ClienteId = @ClienteId ORDER BY p.Tipo, p.Numero;

    SELECT b.BloqueoId, b.Codigo, p.Tipo AS Producto, p.Numero AS Cuenta, b.Monto, b.Levantado, b.Transferido, b.Vigente, b.Estado, b.Fecha
    FROM coa.vw_Bloqueo b JOIN coa.Producto p ON p.ProductoId = b.ProductoId
    WHERE b.CoactivaId = @CoactivaId ORDER BY b.BloqueoId;

    SELECT m.Fecha, m.Tipo, f.Numero AS Oficio, f.Token AS OficioToken, CONCAT(p.Tipo, N' ', p.Numero) AS Cuenta, m.Monto, u.Usuario,
           CONCAT_WS(N' · ', CASE WHEN m.FormaPago IS NOT NULL THEN m.FormaPago + N' a ' + ISNULL(m.CuentaDestino, N'—') END,
                     CASE WHEN m.Comprobante IS NOT NULL THEN N'comprobante ' + m.Comprobante END, m.Detalle) AS Detalle
    FROM coa.Movimiento m
    JOIN coa.Bloqueo b ON b.BloqueoId = m.BloqueoId
    JOIN coa.Producto p ON p.ProductoId = b.ProductoId
    JOIN seg.Usuario u ON u.UsuarioId = m.UsuarioId
    LEFT JOIN coa.OrdenCoactiva o ON o.OrdenId = m.OrdenId
    LEFT JOIN coa.Oficio f ON f.OficioId = o.OficioId
    WHERE b.CoactivaId = @CoactivaId
    ORDER BY m.MovimientoId;

    SELECT o.OrdenId, o.Tipo, f.Numero AS Oficio, f.Recibido, f.RemitidoPor, o.MontoOrdenado, o.Ejecutado, o.Estado
    FROM coa.vw_Orden o JOIN coa.Oficio f ON f.OficioId = o.OficioId
    WHERE o.CoactivaId = @CoactivaId AND f.Anulado = 0
    ORDER BY o.OrdenId;
END;
