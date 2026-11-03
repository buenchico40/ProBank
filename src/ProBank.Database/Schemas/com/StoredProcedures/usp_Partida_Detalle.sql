-- Detalle de una partida: sus movimientos, los pagos aplicados (distribución) y los compromisos (solicitudes aprobadas).
CREATE PROCEDURE com.usp_Partida_Detalle @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PartidaId INT = (SELECT PartidaId FROM com.Partida WHERE Token = @Token);

    SELECT m.Codigo, m.Fecha, m.Tipo, CASE WHEN m.PartidaDestinoId = @PartidaId THEN m.Monto ELSE -m.Monto END AS Efecto, m.Estado
    FROM com.Movimiento m WHERE m.PartidaOrigenId = @PartidaId OR m.PartidaDestinoId = @PartidaId
    ORDER BY m.MovimientoId DESC;

    SELECT pg.Codigo AS Pago, s.Codigo AS Solicitud, pg.Fecha, d.Monto, pg.Estado
    FROM com.PagoDistribucion d JOIN com.Pago pg ON pg.PagoId = d.PagoId JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId
    WHERE d.PartidaId = @PartidaId AND pg.Estado <> 'Anulado'
    ORDER BY pg.Fecha DESC, pg.PagoId DESC;

    SELECT s.Codigo, s.Descripcion, s.Monto, s.Pagado, s.PorPagar, s.Estado
    FROM com.vw_Solicitud s
    WHERE s.PartidaId = @PartidaId AND s.Aprobacion = 'Aprobada' AND s.Anulada = 0
    ORDER BY s.SolicitudId DESC;
END;
