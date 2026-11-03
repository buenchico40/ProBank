-- Definición de una consulta para ejecutarla: bases activas permitidas, roles que la ejecutan, aprobadores y parámetros.
CREATE PROCEDURE rpt.usp_Consulta_Obtener @Codigo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id INT = (SELECT ConsultaId FROM rpt.Consulta WHERE Codigo = @Codigo);
    SELECT ConsultaId, Codigo, Nombre, Procedimiento, TiempoMaximoSeg, LimiteFilas, Estado FROM rpt.Consulta WHERE ConsultaId = @Id;
    SELECT c.Codigo, c.Nombre FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId AND c.Estado = 'Activo'
    WHERE cc.ConsultaId = @Id ORDER BY c.Nombre;
    SELECT r.Nombre FROM rpt.ConsultaRol cr JOIN seg.Rol r ON r.RolId = cr.RolId WHERE cr.ConsultaId = @Id;
    SELECT u.UsuarioId, u.Nombre FROM rpt.ConsultaAprobador a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId WHERE a.ConsultaId = @Id ORDER BY u.Nombre;
    SELECT Nombre, Etiqueta, Tipo, Obligatorio, ValorDefecto, Opciones FROM rpt.Parametro WHERE ConsultaId = @Id ORDER BY Orden;
END;
