-- Una ejecución para operarla (la API decide quién la ve): datos internos, aprobadores con su decisión y su bitácora.
CREATE PROCEDURE rpt.usp_Ejecucion_Obtener @Codigo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id BIGINT = (SELECT EjecucionId FROM rpt.Ejecucion WHERE Codigo = @Codigo);
    SELECT e.EjecucionId, e.Codigo, q.Codigo AS ConsultaCodigo, q.Nombre AS ConsultaNombre, q.Procedimiento, c.Codigo AS ConexionCodigo,
           c.Nombre AS Base, e.SolicitanteId, u.Nombre AS SolicitadaPor, e.Fecha, e.ParametrosResueltos, e.ParametrosJson, e.Programada,
           e.Filas, e.DuracionMs, e.Truncado, e.RutaResultado, e.Mensaje, e.Estado
    FROM rpt.Ejecucion e
    JOIN rpt.Consulta q ON q.ConsultaId = e.ConsultaId
    JOIN rpt.Conexion c ON c.ConexionId = e.ConexionId
    JOIN seg.Usuario u ON u.UsuarioId = e.SolicitanteId
    WHERE e.EjecucionId = @Id;
    SELECT a.UsuarioId, u.Nombre, a.Decision, a.Fecha, a.Comentario
    FROM rpt.EjecucionAprobador a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId
    WHERE a.EjecucionId = @Id ORDER BY u.Nombre;
    SELECT ev.Fecha, ISNULL(u.Nombre, N'Programador de consultas') AS Usuario, ev.Evento, ev.Detalle, ev.Filas, ev.DuracionMs, ev.Resultado, ev.Ip
    FROM rpt.EjecucionEvento ev LEFT JOIN seg.Usuario u ON u.UsuarioId = ev.UsuarioId
    WHERE ev.EjecucionId = @Id ORDER BY ev.EventoId;
END;
