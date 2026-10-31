CREATE PROCEDURE rpt.usp_Consulta_Obtener @Codigo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id INT = (SELECT ConsultaId FROM rpt.Consulta WHERE Codigo = @Codigo);
    SELECT ConsultaId, Codigo, Nombre, Procedimiento, TiempoMaximoSeg, LimiteFilas, Estado FROM rpt.Consulta WHERE ConsultaId = @Id;
    SELECT cc.ConexionId FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId AND c.Estado = 'Activo' WHERE cc.ConsultaId = @Id;
    SELECT r.Nombre FROM rpt.ConsultaRol cr JOIN seg.Rol r ON r.RolId = cr.RolId WHERE cr.ConsultaId = @Id;
    SELECT UsuarioId FROM rpt.ConsultaAprobador WHERE ConsultaId = @Id;
    SELECT Nombre, Etiqueta, Tipo, Obligatorio, ValorDefecto FROM rpt.Parametro WHERE ConsultaId = @Id ORDER BY Orden;
END;
