-- Oficio con sus totales y estado: Pendiente (sin órdenes o todas pendientes), Atendido (todas ejecutadas), En curso o Anulado.
CREATE VIEW coa.vw_Oficio
AS
SELECT f.OficioId, f.Token, f.Numero, f.Recibido, f.RemitidoPor, f.Anulado,
       x.Ordenes, x.Bloqueos, x.Levantamientos, x.Transferencias, x.Entidades, x.MontoCoactivas, x.Ejecutado,
       CASE WHEN f.Anulado = 1 THEN 'Anulado'
            WHEN x.Activas = 0 OR x.Pendientes = x.Activas THEN 'Pendiente'
            WHEN x.Ejecutadas = x.Activas THEN 'Atendido'
            ELSE 'En curso' END AS Estado
FROM coa.Oficio f
CROSS APPLY (SELECT COUNT(*) AS Ordenes,
                    ISNULL(SUM(CASE WHEN o.Tipo = 'Bloqueo' THEN 1 ELSE 0 END), 0) AS Bloqueos,
                    ISNULL(SUM(CASE WHEN o.Tipo = 'Levantamiento' THEN 1 ELSE 0 END), 0) AS Levantamientos,
                    ISNULL(SUM(CASE WHEN o.Tipo = 'Transferencia' THEN 1 ELSE 0 END), 0) AS Transferencias,
                    COUNT(DISTINCT c.EntidadId) AS Entidades,
                    ISNULL(SUM(CASE WHEN o.Estado <> 'Anulado' THEN c.Monto END), 0) AS MontoCoactivas,
                    ISNULL(SUM(CASE WHEN o.Estado <> 'Anulado' THEN o.Ejecutado END), 0) AS Ejecutado,
                    ISNULL(SUM(CASE WHEN o.Estado <> 'Anulado' THEN 1 ELSE 0 END), 0) AS Activas,
                    ISNULL(SUM(CASE WHEN o.Estado = 'Pendiente' THEN 1 ELSE 0 END), 0) AS Pendientes,
                    ISNULL(SUM(CASE WHEN o.Estado = 'Ejecutada' THEN 1 ELSE 0 END), 0) AS Ejecutadas
             FROM coa.vw_Orden o JOIN coa.Coactiva c ON c.CoactivaId = o.CoactivaId
             WHERE o.OficioId = f.OficioId) x;
