-- Orden del oficio con lo ejecutado y su estado: bloqueo Pendiente/Ejecutada; levantamiento y transferencia
-- Pendiente/Parcial/Ejecutada frente al monto ordenado. Sin monto = total: sigue Parcial mientras la coactiva tenga
-- fondos bloqueados vigentes (si no, un levantamiento parcial cerraba la orden y ya no se podía ejecutar el resto).
-- Coactiva anulada = Anulado.
CREATE VIEW coa.vw_Orden
AS
SELECT o.OrdenId, o.OficioId, o.CoactivaId, o.Tipo, o.MontoOrdenado, o.Observacion, x.Ejecutado,
       CASE WHEN c.Anulada = 1 THEN 'Anulado'
            WHEN x.Ejecutado <= 0 THEN 'Pendiente'
            WHEN o.Tipo <> 'Bloqueo' AND o.MontoOrdenado IS NOT NULL AND x.Ejecutado < o.MontoOrdenado THEN 'Parcial'
            WHEN o.Tipo <> 'Bloqueo' AND o.MontoOrdenado IS NULL AND v.Vigente > 0 THEN 'Parcial'
            ELSE 'Ejecutada' END AS Estado
FROM coa.OrdenCoactiva o
JOIN coa.Coactiva c ON c.CoactivaId = o.CoactivaId
CROSS APPLY (SELECT ISNULL(SUM(m.Monto), 0) AS Ejecutado FROM coa.Movimiento m WHERE m.OrdenId = o.OrdenId) x
CROSS APPLY (SELECT ISNULL(SUM(b.Vigente), 0) AS Vigente FROM coa.vw_Bloqueo b WHERE b.CoactivaId = o.CoactivaId) v;
