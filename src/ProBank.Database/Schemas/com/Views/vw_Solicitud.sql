-- Solicitud con lo pagado (pagos Pagado), lo que queda por pagar y su estado:
-- Anulado, Pendiente (sin aprobar), Rechazada, En curso (aprobada con saldo) o Completado (pagada por completo).
CREATE VIEW com.vw_Solicitud
AS
SELECT s.SolicitudId, s.Token, s.Codigo, s.Descripcion, s.Area, s.Proveedor, s.Presupuestado, s.PartidaId, s.Monto, s.RenovacionId,
       s.Aprobacion, s.Anulada, s.SolicitanteId, s.FechaRegistro, s.AprobadoPorId, s.FechaAprobacion, s.Comentario,
       x.Pagado, CASE WHEN s.Monto > x.Pagado THEN s.Monto - x.Pagado ELSE 0 END AS PorPagar,
       CASE WHEN s.Anulada = 1 THEN 'Anulado'
            WHEN s.Aprobacion = 'Pendiente' THEN 'Pendiente'
            WHEN s.Aprobacion = 'Rechazada' THEN 'Rechazada'
            WHEN x.Pagado >= s.Monto THEN 'Completado'
            ELSE 'En curso' END AS Estado
FROM com.Solicitud s
CROSS APPLY (SELECT ISNULL(SUM(p.Monto), 0) AS Pagado FROM com.Pago p WHERE p.SolicitudId = s.SolicitudId AND p.Estado = 'Pagado') x;
