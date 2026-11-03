-- SQL-01: catálogo de consultas con sus bases, roles y aprobadores en texto.
CREATE PROCEDURE rpt.usp_Consulta_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT q.Codigo, q.Nombre, q.Descripcion, q.Procedimiento,
           ISNULL((SELECT STRING_AGG(c.Nombre, ', ') WITHIN GROUP (ORDER BY c.Nombre) FROM rpt.ConsultaConexion cc
                   JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId WHERE cc.ConsultaId = q.ConsultaId), '') AS Conexiones,
           ISNULL((SELECT STRING_AGG(r.Nombre, ', ') WITHIN GROUP (ORDER BY r.Nombre) FROM rpt.ConsultaRol cr
                   JOIN seg.Rol r ON r.RolId = cr.RolId WHERE cr.ConsultaId = q.ConsultaId), '') AS Roles,
           ISNULL((SELECT STRING_AGG(u.Nombre, ', ') WITHIN GROUP (ORDER BY u.Nombre) FROM rpt.ConsultaAprobador a
                   JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId WHERE a.ConsultaId = q.ConsultaId), '') AS Aprobadores,
           q.TiempoMaximoSeg, q.LimiteFilas, q.Estado,
           (SELECT COUNT(*) FROM rpt.Parametro p WHERE p.ConsultaId = q.ConsultaId) AS Parametros,
           CAST(CASE WHEN q.DefinicionLeida IS NULL THEN 0 ELSE 1 END AS BIT) AS DefinicionLeida
    FROM rpt.Consulta q
    ORDER BY q.Codigo;
END;
