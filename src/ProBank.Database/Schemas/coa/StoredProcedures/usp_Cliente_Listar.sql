-- Coactivas › Coactivas por cliente: totales de sus coactivas no anuladas y cobertura conjunta.
CREATE PROCEDURE coa.usp_Cliente_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cl.Identificacion, cl.TipoIdentificacion, cl.Nombre AS Cliente,
           COUNT(c.CoactivaId) AS Coactivas, COUNT(DISTINCT c.OficioId) AS Oficios, COUNT(DISTINCT c.EntidadId) AS Entidades,
           ISNULL(SUM(c.Monto), 0) AS MontoTotal, ISNULL(SUM(c.Bloqueado), 0) AS Bloqueado,
           CASE WHEN ISNULL(SUM(c.Bloqueado + c.Transferido), 0) <= 0 THEN 'Sin bloqueo'
                WHEN SUM(c.Bloqueado + c.Transferido) >= SUM(c.Monto) THEN 'Total' ELSE 'Parcial' END AS Cobertura
    FROM coa.Cliente cl
    JOIN coa.vw_Coactiva c ON c.ClienteId = cl.ClienteId AND c.Anulada = 0
    GROUP BY cl.ClienteId, cl.Identificacion, cl.TipoIdentificacion, cl.Nombre
    ORDER BY cl.Nombre;
END;
