-- Ficha de una consulta: datos, bases, roles, aprobadores, parámetros configurados, últimas ejecuciones y envíos programados.
CREATE PROCEDURE rpt.usp_Consulta_Detalle @Codigo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id INT = (SELECT ConsultaId FROM rpt.Consulta WHERE Codigo = @Codigo);
    SELECT q.Codigo, q.Nombre, q.Descripcion, q.Procedimiento, q.TiempoMaximoSeg, q.LimiteFilas, q.Estado, q.ColumnasJson, q.DefinicionLeida,
           u.Nombre AS CreadoPor, q.FechaRegistro
    FROM rpt.Consulta q JOIN seg.Usuario u ON u.UsuarioId = q.CreadoPorId
    WHERE q.ConsultaId = @Id;
    SELECT c.Codigo, c.Nombre, c.Estado FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId WHERE cc.ConsultaId = @Id ORDER BY c.Nombre;
    SELECT r.Nombre FROM rpt.ConsultaRol cr JOIN seg.Rol r ON r.RolId = cr.RolId WHERE cr.ConsultaId = @Id ORDER BY r.Nombre;
    SELECT u.Token, u.Nombre FROM rpt.ConsultaAprobador a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId WHERE a.ConsultaId = @Id ORDER BY u.Nombre;
    SELECT Nombre, Etiqueta, Tipo, TipoSql, Obligatorio, ValorDefecto, Opciones FROM rpt.Parametro WHERE ConsultaId = @Id ORDER BY Orden;
    SELECT TOP (5) e.Codigo, c.Nombre AS Base, u.Nombre AS SolicitadaPor, e.Fecha,
           CASE e.Estado WHEN 'PendienteAprobacion' THEN 'Pendiente de aprobación' ELSE e.Estado END AS Estado
    FROM rpt.Ejecucion e JOIN rpt.Conexion c ON c.ConexionId = e.ConexionId JOIN seg.Usuario u ON u.UsuarioId = e.SolicitanteId
    WHERE e.ConsultaId = @Id ORDER BY e.EjecucionId DESC;
    SELECT p.Codigo, c.Codigo AS ConexionCodigo, c.Nombre AS Base, p.Frecuencia, CAST(p.Hora AS TIME(0)) AS Hora, p.Dias,
           CAST(p.DiaDelMes AS INT) AS DiaDelMes, p.Formato, p.ParametrosJson,
           ISNULL((SELECT STRING_AGG(d.Correo, ', ') WITHIN GROUP (ORDER BY d.Correo) FROM rpt.ProgramacionDestinatario d WHERE d.ProgramacionId = p.ProgramacionId), '') AS Destinatarios,
           u.Nombre AS Responsable, p.ProximaEjecucion, p.UltimaEjecucion, p.UltimoResultado, p.Estado
    FROM rpt.Programacion p JOIN rpt.Conexion c ON c.ConexionId = p.ConexionId JOIN seg.Usuario u ON u.UsuarioId = p.ResponsableId
    WHERE p.ConsultaId = @Id ORDER BY p.ProgramacionId;
END;
