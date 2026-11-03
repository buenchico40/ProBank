-- Pagos con su solicitud y su distribución (códigos de las partidas y lo distribuido). Con @Token devuelve solo ese y, en dos
-- resultados más, su distribución por partida y sus documentos.
CREATE PROCEDURE com.usp_Pago_Listar @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pg.Token, pg.Codigo, s.Token AS SolicitudToken, s.Codigo AS Solicitud, s.Descripcion AS SolicitudDescripcion, s.Presupuestado,
           pg.Proveedor, pg.Fecha, pg.Monto, pg.Estado,
           ISNULL((SELECT STRING_AGG(pa.Codigo, ', ') WITHIN GROUP (ORDER BY pa.Codigo) FROM com.PagoDistribucion d JOIN com.Partida pa ON pa.PartidaId = d.PartidaId WHERE d.PagoId = pg.PagoId), '') AS Partidas,
           ISNULL((SELECT SUM(d.Monto) FROM com.PagoDistribucion d WHERE d.PagoId = pg.PagoId), 0) AS Distribuido,
           u.Nombre AS RegistradoPor, pg.FechaRegistro, x.Nombre AS PagadoPor, pg.FechaPagado,
           (SELECT COUNT(*) FROM com.Documento dc WHERE dc.PagoId = pg.PagoId) AS Documentos
    FROM com.Pago pg
    JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId
    JOIN seg.Usuario u ON u.UsuarioId = pg.RegistradoPorId
    LEFT JOIN seg.Usuario x ON x.UsuarioId = pg.PagadoPorId
    WHERE @Token IS NULL OR pg.Token = @Token
    ORDER BY pg.PagoId DESC;

    IF @Token IS NULL RETURN;
    SELECT pa.Token AS PartidaToken, pa.Codigo AS Partida, pa.Rubro, d.Monto
    FROM com.PagoDistribucion d JOIN com.Partida pa ON pa.PartidaId = d.PartidaId JOIN com.Pago pg ON pg.PagoId = d.PagoId
    WHERE pg.Token = @Token ORDER BY pa.Codigo;

    EXEC com.usp_Documento_Listar 'Pago', @Token;
END;
