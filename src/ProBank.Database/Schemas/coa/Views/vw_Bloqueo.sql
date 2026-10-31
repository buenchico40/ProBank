-- Bloqueo con lo levantado, transferido y vigente (monto − levantado − transferido) y su estado.
CREATE VIEW coa.vw_Bloqueo
AS
SELECT b.BloqueoId, b.Codigo, b.CoactivaId, b.ProductoId, b.Monto, b.Fecha, x.Levantado, x.Transferido,
       b.Monto - x.Levantado - x.Transferido AS Vigente,
       CASE WHEN b.Monto - x.Levantado - x.Transferido >= b.Monto THEN 'Bloqueado'
            WHEN b.Monto - x.Levantado - x.Transferido > 0 THEN 'Bloqueo parcial'
            WHEN x.Transferido > 0 AND x.Levantado > 0 THEN 'Transferido y liberado'
            WHEN x.Transferido > 0 THEN 'Transferido'
            ELSE 'Liberado' END AS Estado
FROM coa.Bloqueo b
CROSS APPLY (SELECT ISNULL(SUM(CASE WHEN m.Tipo = 'Levantamiento' THEN m.Monto END), 0) AS Levantado,
                    ISNULL(SUM(CASE WHEN m.Tipo = 'Transferencia' THEN m.Monto END), 0) AS Transferido
             FROM coa.Movimiento m WHERE m.BloqueoId = b.BloqueoId) x;
