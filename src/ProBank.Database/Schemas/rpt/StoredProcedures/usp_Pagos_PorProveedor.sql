-- Reporte para Consultas dinámicas: pagos a proveedores de un rango de fechas (Presupuesto), con su solicitud y partidas.
-- @Proveedor opcional (contiene el texto).
CREATE PROCEDURE rpt.usp_Pagos_PorProveedor @Desde DATE, @Hasta DATE, @Proveedor NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pg.Codigo AS Pago, pg.Fecha, pg.Proveedor, s.Codigo AS Solicitud, s.Descripcion,
           CAST(s.Presupuestado AS BIT) AS Presupuestado,
           ISNULL((SELECT STRING_AGG(pa.Codigo, ', ') FROM com.PagoDistribucion d JOIN com.Partida pa ON pa.PartidaId = d.PartidaId WHERE d.PagoId = pg.PagoId), '') AS Partidas,
           pg.Monto, pg.Estado
    FROM com.Pago pg
    JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId
    WHERE pg.Fecha BETWEEN @Desde AND @Hasta
      AND (NULLIF(@Proveedor, N'') IS NULL OR pg.Proveedor LIKE N'%' + @Proveedor + N'%')
    ORDER BY pg.Fecha, pg.Codigo;
END;
