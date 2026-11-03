-- Consultas publicadas que puede ejecutar un rol (el Administrador, todas), con sus bases activas y si requieren aprobación.
CREATE PROCEDURE rpt.usp_Consulta_Disponibles @RolActivo VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT q.Codigo, q.Nombre, q.Descripcion, q.Procedimiento,
           (SELECT COUNT(*) FROM rpt.ConsultaAprobador a WHERE a.ConsultaId = q.ConsultaId) AS Aprobadores
    FROM rpt.Consulta q
    WHERE q.Estado = 'Publicada'
      AND (@RolActivo = 'Administrador'
           OR EXISTS (SELECT 1 FROM rpt.ConsultaRol cr JOIN seg.Rol r ON r.RolId = cr.RolId WHERE cr.ConsultaId = q.ConsultaId AND r.Nombre = @RolActivo))
      AND EXISTS (SELECT 1 FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId AND c.Estado = 'Activo' WHERE cc.ConsultaId = q.ConsultaId)
    ORDER BY q.Codigo;
END;
