-- Ejecuciones que ve un usuario: las que solicitó y las que le toca aprobar (con @Todas, todas: auditoría de ejecuciones).
-- MeToca: está pendiente y el usuario aún no decide como aprobador.
CREATE PROCEDURE rpt.usp_Ejecucion_Listar @UsuarioId INT, @Todas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.Codigo, q.Codigo AS ConsultaCodigo, q.Nombre AS ConsultaNombre, c.Nombre AS Base, e.ParametrosResueltos AS Parametros,
           u.Nombre AS SolicitadaPor, e.Fecha,
           (SELECT COUNT(*) FROM rpt.EjecucionAprobador a WHERE a.EjecucionId = e.EjecucionId AND a.Decision = 'Aprobada') AS Aprobadas,
           (SELECT COUNT(*) FROM rpt.EjecucionAprobador a WHERE a.EjecucionId = e.EjecucionId) AS Aprobadores,
           e.Filas, CASE e.Estado WHEN 'PendienteAprobacion' THEN 'Pendiente de aprobación' ELSE e.Estado END AS Estado, e.Programada,
           CAST(CASE WHEN e.SolicitanteId = @UsuarioId THEN 1 ELSE 0 END AS BIT) AS EsMia,
           CAST(CASE WHEN e.Estado = 'PendienteAprobacion' AND EXISTS (SELECT 1 FROM rpt.EjecucionAprobador a
                    WHERE a.EjecucionId = e.EjecucionId AND a.UsuarioId = @UsuarioId AND a.Decision IS NULL) THEN 1 ELSE 0 END AS BIT) AS MeToca,
           CAST(CASE WHEN e.RutaResultado IS NULL THEN 0 ELSE 1 END AS BIT) AS TieneResultado
    FROM rpt.Ejecucion e
    JOIN rpt.Consulta q ON q.ConsultaId = e.ConsultaId
    JOIN rpt.Conexion c ON c.ConexionId = e.ConexionId
    JOIN seg.Usuario u ON u.UsuarioId = e.SolicitanteId
    WHERE @Todas = 1 OR e.SolicitanteId = @UsuarioId
       OR EXISTS (SELECT 1 FROM rpt.EjecucionAprobador a WHERE a.EjecucionId = e.EjecucionId AND a.UsuarioId = @UsuarioId)
    ORDER BY e.EjecucionId DESC;
END;
