-- Ejecución de cada partida (como recomputeBudget del prototipo):
--   Ajustes      = movimientos aprobados (entra como destino, sale como origen)
--   Comprometido = solicitudes presupuestadas aprobadas y no anuladas de la partida, por lo que les falta pagar
--   Pagado       = distribución de los pagos Pagado aplicada a la partida (COM-04)
--   Disponible   = Asignado + Ajustes − Comprometido − Pagado; Ejecución = (Pagado + Comprometido) ÷ (Asignado + Ajustes)
CREATE VIEW com.vw_Partida
AS
SELECT p.PartidaId, p.Token, p.Codigo, p.Anio, p.Area, p.Rubro, p.Asignado, p.Estado, p.CreadoPorId, p.FechaRegistro, p.AprobadoPorId, p.FechaAprobacion,
       a.Ajustes, c.Comprometido, g.Pagado,
       p.Asignado + a.Ajustes - c.Comprometido - g.Pagado AS Disponible,
       CASE WHEN p.Asignado + a.Ajustes > 0 THEN CAST(ROUND(100.0 * (g.Pagado + c.Comprometido) / (p.Asignado + a.Ajustes), 0) AS INT) ELSE 0 END AS Ejecucion
FROM com.Partida p
CROSS APPLY (SELECT ISNULL((SELECT SUM(m.Monto) FROM com.Movimiento m WHERE m.Estado = 'Aprobada' AND m.PartidaDestinoId = p.PartidaId), 0)
                  - ISNULL((SELECT SUM(m.Monto) FROM com.Movimiento m WHERE m.Estado = 'Aprobada' AND m.PartidaOrigenId = p.PartidaId), 0) AS Ajustes) a
CROSS APPLY (SELECT ISNULL(SUM(s.PorPagar), 0) AS Comprometido
             FROM com.vw_Solicitud s WHERE s.PartidaId = p.PartidaId AND s.Presupuestado = 1 AND s.Aprobacion = 'Aprobada' AND s.Anulada = 0) c
CROSS APPLY (SELECT ISNULL(SUM(d.Monto), 0) AS Pagado
             FROM com.PagoDistribucion d JOIN com.Pago pg ON pg.PagoId = d.PagoId AND pg.Estado = 'Pagado' WHERE d.PartidaId = p.PartidaId) g;
