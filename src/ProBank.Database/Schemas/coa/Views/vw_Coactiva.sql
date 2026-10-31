-- Coactiva con montos y COA-05: cobertura = (bloqueado vigente + transferido) frente al monto.
-- Estado: Anulado, Pendiente (sin bloqueos), Activo (con fondos bloqueados), Levantada o Transferida (sin fondos vigentes).
CREATE VIEW coa.vw_Coactiva
AS
SELECT c.CoactivaId, c.Token, c.OficioId, c.EntidadId, c.ClienteId, c.NumeroProceso, c.Monto, c.ProductosPermitidos, c.Observacion, c.Anulada,
       x.Bloqueado, x.Levantado, x.Transferido, x.Bloqueos,
       CASE WHEN c.Anulada = 1 THEN '—'
            WHEN x.Bloqueado + x.Transferido <= 0 THEN 'Sin bloqueo'
            WHEN x.Bloqueado + x.Transferido >= c.Monto THEN 'Total'
            ELSE 'Parcial' END AS Cobertura,
       CASE WHEN c.Anulada = 1 THEN 'Anulado'
            WHEN x.Bloqueos = 0 THEN 'Pendiente'
            WHEN x.Bloqueado > 0 THEN 'Activo'
            WHEN x.Transferido > 0 THEN 'Transferida'
            ELSE 'Levantada' END AS Estado
FROM coa.Coactiva c
CROSS APPLY (SELECT ISNULL(SUM(b.Vigente), 0) AS Bloqueado, ISNULL(SUM(b.Levantado), 0) AS Levantado,
                    ISNULL(SUM(b.Transferido), 0) AS Transferido, COUNT(b.BloqueoId) AS Bloqueos
             FROM coa.vw_Bloqueo b WHERE b.CoactivaId = c.CoactivaId) x;
